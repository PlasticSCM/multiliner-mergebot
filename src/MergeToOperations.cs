using System;
using System.Collections.Generic;

using log4net;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Api.Requests;
using MultilinerBot.Api.Responses;
using MultilinerBot.Configuration;

namespace MultilinerBot
{
    internal static class MergeToOperations
    {
        internal class ShelveResult
        {
            internal Dictionary<string, int> ShelvesByTargetBranch = new Dictionary<string, int>();
            internal Dictionary<string, MergeToResultStatus> MergeStatusByTargetBranch = new Dictionary<string, MergeToResultStatus>();
            internal List<string> ErrorMessages = new List<string>();
            internal List<string> MergesNotNeededMessages = new List<string>();
        }

        internal class CheckinResult
        {
            internal Dictionary<string, int> ChangesetsByTargetBranch = new Dictionary<string, int>();
            internal Dictionary<string, MergeToResultStatus> MergeStatusByTargetBranch = new Dictionary<string, MergeToResultStatus>();
            internal List<string> ErrorMessages = new List<string>();
            internal List<string> DestinationNewChangesWarnings = new List<string>();
        }

        internal static ShelveResult TryMergeToShelves(
            IRestApi restApi,
            Branch branch,
            string[] destinationBranches,
            MergeReport mergeReport,
            string taskTitle,
            string botName)
        {
            ShelveResult result = new ShelveResult();

            foreach (string destinationBranch in destinationBranches)
            {
                MergeToResponse mergeToResult = MultilinerMergebotApi.MergeBranchTo(
                    restApi,
                    branch.Repository,
                    branch.FullName,
                    destinationBranch,
                    GetComment(branch.FullName, destinationBranch, taskTitle, botName),
                    MultilinerMergebotApi.MergeToOptions.CreateShelve);

                result.MergeStatusByTargetBranch[destinationBranch] = mergeToResult.Status;

                if (mergeToResult.Status == MergeToResultStatus.MergeNotNeeded)
                {
                    string warningMessage = string.Format(
                        "Branch [{0}] was already merged to [{1}] (No merge needed).",
                        branch.FullName,
                        destinationBranch);

                    result.MergesNotNeededMessages.Add(warningMessage);
                    continue;
                }

                if (IsFailedMergeTo(mergeToResult))
                {
                    string errorMsg = string.Format(
                        "Can't merge branch [{0}] to [{1}]. Reason: {2}.",
                        branch.FullName,
                        destinationBranch,
                        mergeToResult.Message);

                    result.ErrorMessages.Add(errorMsg);

                    BuildMergeReport.AddFailedMergeProperty(
                        mergeReport, mergeToResult.Status, mergeToResult.Message);

                    continue;
                }

                result.ShelvesByTargetBranch[destinationBranch] = mergeToResult.ChangesetNumber;
                BuildMergeReport.AddSucceededMergeProperty(mergeReport, mergeToResult.Status);
            }
            return result;
        }

        internal static CheckinResult TryApplyShelves(
            IRestApi restApi,
            Branch branch,
            string[] destinationBranches,
            ShelveResult shelves,
            MergeReport mergeReport,
            string taskNumber,
            string taskTitle,
            string botName,
            MultilinerBotConfiguration botConfig,
            string codeReviewsStorageFile)
        {
            CheckinResult result = new CheckinResult();
            int shelveId;

            foreach (string destinationBranch in destinationBranches)
            {
                if (!shelves.ShelvesByTargetBranch.ContainsKey(destinationBranch))
                    continue;

                shelveId = shelves.ShelvesByTargetBranch[destinationBranch];

                mLog.InfoFormat(
                    "Checking-in shelveset [{0}] from branch [{1}] to [{2}]",
                    shelveId, branch.FullName, destinationBranch);

                MergeToResponse mergeResult = MultilinerMergebotApi.MergeShelveTo(
                    restApi,
                    branch.Repository,
                    shelveId,
                    destinationBranch,
                    GetComment(branch.FullName, destinationBranch, taskTitle, botName),
                    MultilinerMergebotApi.MergeToOptions.EnsureNoDstChanges);

                result.MergeStatusByTargetBranch[destinationBranch] = mergeResult.Status;

                BuildMergeReport.UpdateMergeProperty(mergeReport, mergeResult.Status, mergeResult.ChangesetNumber);

                if (mergeResult.Status == MergeToResultStatus.OK)
                {
                    result.ChangesetsByTargetBranch[destinationBranch] = mergeResult.ChangesetNumber;

                    mLog.InfoFormat(
                        "Checkin: Created changeset [{0}] in branch [{1}]",
                        mergeResult.ChangesetNumber, destinationBranch);

                    continue;
                }

                if (mergeResult.Status == MergeToResultStatus.DestinationChanges)
                {
                    string dstWarnMessage = string.Format(
                        "Can't checkin shelve [{0}], the resulting shelve from merging branch " +
                        "[{1}] to [{2}]. Reason: new changesets appeared in destination branch " +
                         "while mergebot {3} was processing the merge from [{1}] to [{2}].{4}{5}",
                        shelveId,
                        branch.FullName,
                        destinationBranch,
                        botName,
                        Environment.NewLine,
                        mergeResult.Message);

                    // it should checkin the shelve only on the exact parent shelve cset.
                    // if there are new changes in the destination branch enqueue again the task
                    result.DestinationNewChangesWarnings.Add(dstWarnMessage);
                    continue;
                }

                string errorMsg = string.Format(
                    "Can't checkin shelve [{0}], the resulting shelve from merging branch " +
                    "[{1}] to [{2}]. Reason: {3}",
                    shelveId,
                    branch.FullName,
                    destinationBranch,
                    mergeResult.Message);

                result.ErrorMessages.Add(errorMsg);
            }

            return result;
        }

        internal static void SafeDeleteShelves(
            IRestApi restApi,
            string repository,
            string[] destinationBranches,
            ShelveResult mergesToShelvesResult)
        {
            if (destinationBranches == null ||
                destinationBranches.Length == 0 ||
                mergesToShelvesResult == null ||
                mergesToShelvesResult.ShelvesByTargetBranch == null)
            {
                return;
            }

            int shelveId = -1;
            foreach (string destinationBranch in destinationBranches)
            {
                if (!mergesToShelvesResult.ShelvesByTargetBranch.ContainsKey(destinationBranch))
                    continue;

                shelveId = mergesToShelvesResult.ShelvesByTargetBranch[destinationBranch];

                if (shelveId == -1)
                    continue;

                try
                {
                    MultilinerMergebotApi.DeleteShelve(restApi, repository, shelveId);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat(
                        "Unable to delete shelve {0} on repository '{1}': {2}",
                        shelveId, repository, ex.Message);

                    mLog.DebugFormat(
                        "StackTrace:{0}{1}",
                        Environment.NewLine, ex.StackTrace);
                }
            }
        }

        static string GetComment(
            string srcBranch,
            string dstBranch,
            string taskTitle,
            string botName)
        {
            string comment = string.Format(
                "Mergebot [{0}]: Merged [{1}{3}] to [{2}]",
                botName,
                srcBranch,
                dstBranch,
                string.IsNullOrWhiteSpace(taskTitle) ? string.Empty : " - " + taskTitle);

            return comment;
        }

        static bool IsFailedMergeTo(MergeToResponse mergeBranchResult)
        {
            return mergeBranchResult.Status == MergeToResultStatus.AncestorNotFound ||
                mergeBranchResult.Status == MergeToResultStatus.Conflicts ||
                mergeBranchResult.Status == MergeToResultStatus.Error ||
                mergeBranchResult.ChangesetNumber == 0;
        }

        static readonly ILog mLog = LogManager.GetLogger("MergeToOperations");
    }
}
