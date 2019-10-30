using System;
using System.Collections.Generic;

using log4net;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Api.Requests;
using MultilinerBot.Api.Responses;
using MultilinerBot.Configuration;

namespace MultilinerBot
{
    public static class ProcessBranch
    {
        public enum Result
        {
            NotReady,
            Ok,
            Failed
        };

        public static Result TryProcessBranch(
            IRestApi restApi,
            Branch branch,
            MultilinerBotConfiguration botConfig,
            string botName,
            string codeReviewsStorageFile)
        {
            string taskNumber = null;
            MergeReport mergeReport = null;
            string[] destinationBranches = null;
            MergeToOperations.ShelveResult mergesToShelves = null;
            string notificationMsg = null;

            try
            {
                mLog.InfoFormat("Getting task number of branch {0} ...", branch.FullName);

                taskNumber = GetTaskNumber(branch.FullName, botConfig.BranchPrefix);

                if (!IsTaskReady(
                    restApi,
                    taskNumber,
                    botConfig.Issues,
                    botConfig.Plastic.IsApprovedCodeReviewFilterEnabled,
                    branch.Repository,
                    branch.Id,
                    codeReviewsStorageFile))
                {
                    return Result.NotReady;
                }

                destinationBranches = GetMergeToDestinationBranches(
                    restApi,
                    branch,
                    botConfig.MergeToBranchesAttrName);

                if (destinationBranches == null || destinationBranches.Length == 0)
                {
                    notificationMsg = string.Format(
                        "The attribute [{0}] of branch [{1}@{2}@{3}] is not properly set. " +
                        "Branch [{1}@{2}@{3}] status will be set as 'failed': [{4}].",
                        botConfig.MergeToBranchesAttrName,
                        branch.FullName,
                        branch.Repository,
                        botConfig.Server,
                        botConfig.Plastic.StatusAttribute.FailedValue);

                    mLog.Warn(notificationMsg);

                    ChangeTaskStatus.SetTaskAsFailed(
                        restApi, branch, taskNumber, botConfig, codeReviewsStorageFile);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.Failed;
                }

                foreach (string destinationBranch in destinationBranches)
                {
                    if (ExistsBranch(restApi, destinationBranch, branch.Repository))
                        continue;

                    notificationMsg = string.Format(
                        "The destination branch [{0}@{1}@{2}] specified in attribute [{3}] " +
                        "of branch [{4}@{1}@{2}] does not exist. " +
                        "Branch [{4}@{1}@{2}] status will be set as 'failed': [{5}].",
                        destinationBranch,
                        branch.Repository,
                        botConfig.Server,
                        botConfig.MergeToBranchesAttrName,
                        branch.FullName,
                        botConfig.Plastic.StatusAttribute.FailedValue);

                    mLog.Warn(notificationMsg);

                    ChangeTaskStatus.SetTaskAsFailed(
                        restApi, branch, taskNumber, botConfig, codeReviewsStorageFile);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.Failed;
                }

                foreach (string destinationBranch in destinationBranches)
                {
                    if (IsMergeAllowed(restApi, branch, destinationBranch))
                        continue;

                    return Result.NotReady;
                }

                mLog.InfoFormat("Building the merge report of task {0} ...", taskNumber);

                mergeReport = BuildMergeReport.Build(
                    MultilinerMergebotApi.GetBranch(restApi, branch.Repository, branch.FullName));

                string taskTittle;
                string taskUrl;

                if (GetIssueInfo(
                    restApi,
                    taskNumber,
                    botConfig.Issues,
                    out taskTittle,
                    out taskUrl))
                {
                    BuildMergeReport.AddIssueProperty(mergeReport, taskTittle, taskUrl);
                }

                mLog.InfoFormat("Trying to shelve server-side-merge from [{0}] to [{1}]",
                    branch.FullName, string.Join(", ", destinationBranches));

                mergesToShelves =
                    MergeToOperations.TryMergeToShelves(
                        restApi,
                        branch,
                        destinationBranches,
                        mergeReport,
                        taskTittle,
                        botName);

                if (NoMergesNeeded(mergesToShelves))
                {
                    ChangeTaskStatus.Result chStatResult = ChangeTaskStatus.SetTaskAsMerged(
                        restApi,
                        branch,
                        taskNumber,
                        botConfig,
                        codeReviewsStorageFile);

                    notificationMsg = string.Join(Environment.NewLine, mergesToShelves.MergesNotNeededMessages);

                    if (!string.IsNullOrWhiteSpace(chStatResult.ErrorMessage))
                        notificationMsg = string.Concat(
                            notificationMsg, Environment.NewLine, chStatResult.ErrorMessage);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.Failed;
                }

                if (mergesToShelves.ErrorMessages.Count > 0)
                {
                    ChangeTaskStatus.Result chStatResult = ChangeTaskStatus.SetTaskAsFailed(
                        restApi,
                        branch,
                        taskNumber,
                        botConfig,
                        codeReviewsStorageFile);

                    notificationMsg = string.Join(Environment.NewLine, mergesToShelves.ErrorMessages);

                    if (mergesToShelves.MergesNotNeededMessages.Count > 0)
                        notificationMsg = string.Concat(
                            notificationMsg,
                            Environment.NewLine,
                            string.Join(Environment.NewLine, mergesToShelves.MergesNotNeededMessages));

                    if (!string.IsNullOrWhiteSpace(chStatResult.ErrorMessage))
                        notificationMsg = string.Concat(
                            notificationMsg, Environment.NewLine, chStatResult.ErrorMessage);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.Failed;
                }

                if (mergesToShelves.MergesNotNeededMessages.Count > 0)
                {
                    string alreadyMergedMessage = string.Join(
                        Environment.NewLine + "\t", mergesToShelves.MergesNotNeededMessages);

                    notificationMsg = string.Format(
                        "Branch [{0}] is already merged to some " +
                        "of the specified destination branches in the attribute [{1}]. " +
                        "The {2} mergebot will continue building the " +
                        "merge(s) from branch [{0}] to [{3}].{4}{4}" +
                        "Report of already merged branches:{4}\t{5}",
                        branch.FullName,
                        botConfig.MergeToBranchesAttrName,
                        botName,
                        string.Join(", ", mergesToShelves.ShelvesByTargetBranch.Keys),
                        Environment.NewLine,
                        alreadyMergedMessage);

                    mLog.Info(notificationMsg);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);
                }

                bool allBuildsOk = Build.PreCheckinStage(
                    restApi,
                    branch,
                    mergeReport,
                    taskNumber,
                    destinationBranches,
                    mergesToShelves,
                    botConfig,
                    codeReviewsStorageFile);

                if (!allBuildsOk)
                {
                    return Result.Failed;
                }

                MergeToOperations.CheckinResult mergesToCheckins = MergeToOperations.TryApplyShelves(
                    restApi,
                    branch,
                    destinationBranches,
                    mergesToShelves,
                    mergeReport,
                    taskNumber,
                    taskTittle,
                    botName,
                    botConfig,
                    codeReviewsStorageFile);

                //checkin went OK in all target branches
                if (mergesToCheckins.ErrorMessages.Count == 0 && 
                    mergesToCheckins.DestinationNewChangesWarnings.Count == 0)
                {
                    mLog.InfoFormat("Setting branch {0} as 'integrated'...", branch.FullName);
                    ChangeTaskStatus.SetTaskAsMerged(
                        restApi,
                        branch,
                        taskNumber,
                        botConfig,
                        codeReviewsStorageFile);

                    notificationMsg = string.Format(
                        "OK: Branch [{0}] was successfully merged to [{1}]",
                        branch.FullName,
                        string.Join(", ", mergesToShelves.ShelvesByTargetBranch.Keys));

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Build.PostCheckinStage(
                        restApi,
                        branch,
                        mergeReport,
                        taskNumber,
                        destinationBranches,
                        mergesToCheckins,
                        botConfig,
                        codeReviewsStorageFile);
                }

                //some of the checkins went wrong -> we must run the post-ci plan for the successful ones,
                //but force-set as failed the source branch (or requeue if the errors are due to New DstChanges
                if (mergesToCheckins.ErrorMessages.Count > 0)
                {
                    ChangeTaskStatus.Result chStatResult = ChangeTaskStatus.SetTaskAsFailed(
                        restApi,
                        branch,
                        taskNumber,
                        botConfig,
                        codeReviewsStorageFile);

                    string checkinErrorMessage = string.Join(
                        Environment.NewLine + "\t", mergesToCheckins.ErrorMessages);

                    if (mergesToCheckins.DestinationNewChangesWarnings.Count > 0)
                    {
                        string dstChangesErrorMessage = string.Join(
                             Environment.NewLine + "\t", mergesToCheckins.DestinationNewChangesWarnings);

                        checkinErrorMessage = string.Concat(
                            checkinErrorMessage,
                            Environment.NewLine + "\t",
                            dstChangesErrorMessage);
                    }

                    notificationMsg = string.Format(
                        "Failed build. The result of building merges from branch [{0}] to [{1}] went OK, " +
                        "but there were some errors checking-in the resulting shelves:{2}\t{3}{2}{2}{4}",
                        branch.FullName,
                        string.Join(", ", mergesToShelves.ShelvesByTargetBranch.Keys),
                        Environment.NewLine,
                        checkinErrorMessage,
                        string.IsNullOrWhiteSpace(chStatResult.ErrorMessage) ?
                            string.Empty : chStatResult.ErrorMessage);

                    mLog.Warn(notificationMsg);

                    if (mergesToCheckins.ChangesetsByTargetBranch.Count == 0)
                    {
                        Notifier.NotifyTaskStatus(
                            restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                        return Result.Failed;
                    }

                    Build.PostCheckinStage(
                        restApi,
                        branch,
                        mergeReport,
                        taskNumber,
                        destinationBranches,
                        mergesToCheckins,
                        botConfig,
                        codeReviewsStorageFile);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.Failed;
                }

                //LAST block -> if (mergesToCheckins.DestinationNewChangesWarnings.Count > 0)
                //Some of the checkins went wrong due to new changeset on at least a destination branch-> 
                //We must run the post-ci plan for the successful ones,
                //but force requeuing of source branch, so the failed merges to dst branches are re-run
                mLog.InfoFormat("Setting branch {0} as 'resolved' (enqueue) ...", branch.FullName);
                MultilinerMergebotApi.ChangeBranchAttribute(
                    restApi, branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.ResolvedValue);

                string dstBranchesNewCsetsMsg = string.Join(
                    Environment.NewLine + "\t", mergesToCheckins.DestinationNewChangesWarnings);

                notificationMsg = string.Format(
                    "Branch [{0}] will be enqueued again, as new changesets appeared in " +
                    "merge destination branches, and thus, the branch needs to be tested again to " +
                    "include those new changesets in the merge. Full report:{1}\t{2}",
                    branch.FullName,
                    Environment.NewLine,
                    dstBranchesNewCsetsMsg);

                mLog.Warn(notificationMsg);

                if (mergesToCheckins.ChangesetsByTargetBranch.Count == 0)
                {
                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                    return Result.NotReady;
                }

                Build.PostCheckinStage(
                    restApi,
                    branch,
                    mergeReport,
                    taskNumber,
                    destinationBranches,
                    mergesToCheckins,
                    botConfig,
                    codeReviewsStorageFile);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                return Result.NotReady;
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "The attempt to process task {0} failed for branch {1}: {2}",
                    taskNumber, branch.FullName, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}", Environment.NewLine, ex.StackTrace);

                ChangeTaskStatus.SetTaskAsFailed(
                    restApi,
                    branch,
                    taskNumber,
                    botConfig,
                    codeReviewsStorageFile);

                notificationMsg = string.Format(
                    "Can't process branch [{0}] because of an unexpected error: {1}.",
                    branch.FullName,
                    ex.Message);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, notificationMsg, botConfig.Notifiers);

                BuildMergeReport.SetUnexpectedExceptionProperty(mergeReport, ex.Message);

                return Result.Failed;
            }
            finally
            {
                ReportMerge(restApi, branch.Repository, branch.FullName, botName, mergeReport);

                MergeToOperations.SafeDeleteShelves(
                    restApi, branch.Repository, destinationBranches, mergesToShelves);
            }
        }

        static string[] GetMergeToDestinationBranches(
            IRestApi restApi,
            Branch branch,
            string mergeToBranchesAttrName)
        {
            string rawAttrValue = string.Empty;

            try
            {
                rawAttrValue = MultilinerMergebotApi.GetBranchAttribute(
                    restApi, branch.Repository, branch.FullName, mergeToBranchesAttrName);
            }
            catch (Exception e)
            {
                mLog.WarnFormat(
                    "Unable to retrieve attribute [{0}] value from branch [{1}]. Error: {2}",
                    mergeToBranchesAttrName, branch.FullName, e.Message);
                return new string[] { };
            }

            if (string.IsNullOrWhiteSpace(rawAttrValue))
                return new string[] { };

            List<string> destinationBranchesList = new List<string>();
            string[] rawSplittedDestinationBranches = rawAttrValue.Split(
                new char[] { ';', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            string cleanDstBranchName;
            foreach (string rawSplittedDestinationBranch in rawSplittedDestinationBranches)
            {
                cleanDstBranchName = rawSplittedDestinationBranch.Trim();
                if (string.IsNullOrWhiteSpace(cleanDstBranchName))
                    continue;

                if (destinationBranchesList.Contains(cleanDstBranchName))
                    continue;

                destinationBranchesList.Add(cleanDstBranchName);
            }

            return destinationBranchesList.ToArray();
        }

        static bool ExistsBranch(IRestApi restApi, string destinationBranch, string repository)
        {
            BranchModel destinationBranchModel = null;
            try
            {
                destinationBranchModel = MultilinerMergebotApi.GetBranch(
                    restApi, repository, destinationBranch);

                return !string.IsNullOrWhiteSpace(destinationBranchModel.Name);
            }
            catch (Exception e)
            {
                mLog.WarnFormat(
                    "Unable to locate branch name [{0}] in repository [{1}]. Error: {2}",
                    destinationBranch, repository, e.Message);

                return false;
            }
        }

        static bool IsMergeAllowed(IRestApi restApi, Branch branch, string destinationBranch)
        {
            if (MultilinerMergebotApi.IsMergeAllowed(
                restApi, branch.Repository, branch.FullName, destinationBranch))
            {
                return true;
            }

            mLog.WarnFormat(
               "Branch [{0}] is not yet ready to be merged into [{1}] " +
               "Jumping to next branch in the queue...",
               branch.FullName,
               destinationBranch);

            return false;
        }

        static string GetTaskNumber(
            string branch,
            string branchPrefix)
        {
            string branchName = BranchSpec.GetName(branch);

            if (string.IsNullOrEmpty(branchPrefix))
                return branchName;

            if (branchName.StartsWith(branchPrefix,
                    StringComparison.InvariantCultureIgnoreCase))
                return branchName.Substring(branchPrefix.Length);

            return null;
        }

        static bool IsTaskReady(
            IRestApi restApi,
            string taskNumber,
            MultilinerBotConfiguration.IssueTracker issuesConfig,
            bool bIsApprovedCodeReviewFilterEnabled,
            string branchRepository,
            string branchId,
            string codeReviewsStorageFile)
        {
            if (taskNumber == null)
                return false;

            if (issuesConfig == null && !bIsApprovedCodeReviewFilterEnabled)
                return true;

            if (bIsApprovedCodeReviewFilterEnabled &&
                !AreAllCodeReviewsApprovedAtLeastOne(
                    branchRepository, branchId, codeReviewsStorageFile))
            {
                return false;
            }

            if (issuesConfig == null)
                return true;

            mLog.InfoFormat("Checking if issue tracker [{0}] is available...", issuesConfig.Plug);
            if (!MultilinerMergebotApi.Issues.Connected(restApi, issuesConfig.Plug))
            {
                mLog.WarnFormat("Issue tracker [{0}] is NOT available...", issuesConfig.Plug);
                return false;
            }

            mLog.InfoFormat("Checking if task {0} is ready in the issue tracker [{1}].",
                taskNumber, issuesConfig.Plug);

            string status = MultilinerMergebotApi.Issues.GetIssueField(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber, issuesConfig.StatusField.Name);

            mLog.DebugFormat("Issue tracker status for task [{0}]: expected [{1}], was [{2}]",
                taskNumber, issuesConfig.StatusField.ResolvedValue, status);

            return status == issuesConfig.StatusField.ResolvedValue;
        }

        static bool AreAllCodeReviewsApprovedAtLeastOne(
            string branchRepository, string branchId, string codeReviewsStorageFile)
        {
            List<Review> branchReviews =
                ReviewsStorage.GetBranchReviews(branchRepository, branchId, codeReviewsStorageFile);

            if (branchReviews == null || branchReviews.Count == 0)
                return false;

            foreach (Review branchReview in branchReviews)
            {
                if (!branchReview.IsApproved())
                    return false;
            }

            return true;
        }

        static bool GetIssueInfo(
            IRestApi restApi,
            string taskNumber,
            MultilinerBotConfiguration.IssueTracker issuesConfig,
            out string taskTittle,
            out string taskUrl)
        {
            taskTittle = null;
            taskUrl = null;

            if (issuesConfig == null)
                return false;

            mLog.InfoFormat("Obtaining task {0} title...", taskNumber);
            taskTittle = MultilinerMergebotApi.Issues.GetIssueField(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber, issuesConfig.TitleField);

            mLog.InfoFormat("Obtaining task {0} URL...", taskNumber);
            taskUrl = MultilinerMergebotApi.Issues.GetIssueUrl(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber);

            return true;
        }

        static bool NoMergesNeeded(MergeToOperations.ShelveResult mergesToShelves)
        {
            foreach (string dstBranch in mergesToShelves.MergeStatusByTargetBranch.Keys)
            {
                if (mergesToShelves.MergeStatusByTargetBranch[dstBranch] == MergeToResultStatus.MergeNotNeeded)
                    continue;

                return false;
            }
            return true;
        }

        static void ReportMerge(
            IRestApi restApi,
            string repository,
            string branchName,
            string botName,
            MergeReport mergeReport)
        {
            if (mergeReport == null)
                return;

            try
            {
                MultilinerMergebotApi.MergeReports.ReportMerge(restApi, botName, mergeReport);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "Unable to report merge for branch '{0}' on repository '{1}': {2}",
                    branchName, repository, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }
        }

        static bool HasToRunPlanAfterTaskMerged(
            MultilinerBotConfiguration.ContinuousIntegration ciConfig)
        {
            if (ciConfig == null)
                return false;

            return !string.IsNullOrEmpty(ciConfig.PlanAfterCheckin);
        }

        static class Build
        {
            internal static bool PreCheckinStage(
                IRestApi restApi,
                Branch branch,
                MergeReport mergeReport,
                string taskNumber,
                string[] destinationBranches,
                MergeToOperations.ShelveResult mergesToShelves,
                MultilinerBotConfiguration botConfig,
                string codeReviewsStorageFile)
            {
                if (botConfig.CI == null)
                {
                    string noCIMessage =
                        "No Continuous Integration Plug was set for this mergebot. Therefore, no " +
                        "build actions for task " + taskNumber + " will be performed.";

                    mLog.Info(noCIMessage);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, noCIMessage, botConfig.Notifiers);

                    return true;
                }

                if (mergesToShelves == null ||
                    mergesToShelves.ShelvesByTargetBranch == null ||
                    mergesToShelves.ShelvesByTargetBranch.Count == 0)
                {
                    string noShelvesErrorMessage =
                        "Something wrong happened. There are no merge-to shelves to build task " + taskNumber;

                    mLog.Info(noShelvesErrorMessage);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, noShelvesErrorMessage, botConfig.Notifiers);

                    ChangeTaskStatus.SetTaskAsFailed(
                        restApi, branch, taskNumber, botConfig, codeReviewsStorageFile);

                    return false;
                }

                string startTestingMessage = string.Format(
                    "Testing branch [{0}] before being merged in the following destination branches: [{1}].",
                    branch.FullName,
                    string.Join(", ", mergesToShelves.ShelvesByTargetBranch.Keys));

                mLog.Info(startTestingMessage);

                ChangeTaskStatus.SetTaskAsTesting(restApi, branch, taskNumber, botConfig);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, startTestingMessage, botConfig.Notifiers);

                int iniTime = Environment.TickCount;

                BuildOperations.Result result = BuildOperations.TryBuildTask(
                    restApi,
                    branch,
                    mergeReport,
                    taskNumber,
                    destinationBranches,
                    mergesToShelves.ShelvesByTargetBranch,
                    Messages.BuildProperties.StageValues.PRE_CHECKIN,
                    botConfig);

                BuildMergeReport.AddBuildTimeProperty(
                    mergeReport, Environment.TickCount - iniTime);

                if (result.AreAllSuccessful)
                {
                    BuildMergeReport.AddSucceededBuildProperty(
                        mergeReport, botConfig.CI.PlanBranch);

                    return true;
                }

                string errorMessage = string.Join(Environment.NewLine, result.ErrorMessages);

                BuildMergeReport.AddFailedBuildProperty(
                    mergeReport, botConfig.CI.PlanBranch, errorMessage);

                ChangeTaskStatus.SetTaskAsFailed(
                    restApi, branch, taskNumber, botConfig, codeReviewsStorageFile);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, errorMessage, botConfig.Notifiers);

                return false;
            }

            internal static Result PostCheckinStage(
                IRestApi restApi,
                Branch branch,
                MergeReport mergeReport,
                string taskNumber,
                string[] destinationBranches,
                MergeToOperations.CheckinResult mergesToCheckins,
                MultilinerBotConfiguration botConfig,
                string codeReviewsStorageFile)
            {
                if (!HasToRunPlanAfterTaskMerged(botConfig.CI))
                {
                    string noCIMessage =
                        "No Continuous Integration Plug was set for this mergebot. Therefore, no " +
                        "build actions for task " + taskNumber + " will be performed.";

                    mLog.Info(noCIMessage);

                    return Result.Ok;
                }

                if (mergesToCheckins == null ||
                    mergesToCheckins.ChangesetsByTargetBranch == null ||
                    mergesToCheckins.ChangesetsByTargetBranch.Count == 0)
                {
                    string noChangesetsErrorMessage = string.Format(
                        "Something wrong happened. There are no merge-to changesets to build after " +
                        "merging branch [{0}] to its destination branches.", branch.FullName);

                    mLog.Info(noChangesetsErrorMessage);

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, noChangesetsErrorMessage, botConfig.Notifiers);

                    return Result.Failed;
                }

                string startTestingMessage = string.Format(
                    "Testing branch [{0}] after being merged in the following destination branches: [{1}].",
                    branch.FullName,
                    string.Join(", ", mergesToCheckins.ChangesetsByTargetBranch.Keys));

                mLog.Info(startTestingMessage);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, startTestingMessage, botConfig.Notifiers);

                int iniTime = Environment.TickCount;

                BuildOperations.Result result = BuildOperations.TryBuildTask(
                    restApi,
                    branch,
                    mergeReport,
                    taskNumber,
                    destinationBranches,
                    mergesToCheckins.ChangesetsByTargetBranch,
                    Messages.BuildProperties.StageValues.POST_CHECKIN,
                    botConfig);

                BuildMergeReport.AddBuildTimeProperty(
                    mergeReport, Environment.TickCount - iniTime);

                if (result.AreAllSuccessful)
                {
                    BuildMergeReport.AddSucceededBuildProperty(
                        mergeReport, botConfig.CI.PlanAfterCheckin);

                    string notifyOKMesage = string.Format(
                        "Build successful after merging branch [{0}] " +
                        "to the following destination branches: [{1}].",
                        branch.FullName,
                        string.Join(", ", mergesToCheckins.ChangesetsByTargetBranch.Keys));

                    Notifier.NotifyTaskStatus(
                        restApi, branch.Owner, notifyOKMesage, botConfig.Notifiers);

                    return Result.Ok;
                }

                string errorMessage = string.Join(Environment.NewLine, result.ErrorMessages);

                BuildMergeReport.AddFailedBuildProperty(
                    mergeReport, botConfig.CI.PlanAfterCheckin, errorMessage);

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, errorMessage, botConfig.Notifiers);

                return Result.Failed;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("ProcessBranch");
    }
}
