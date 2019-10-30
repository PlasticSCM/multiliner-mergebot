using System.Collections.Generic;

using log4net;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Api.Requests;
using MultilinerBot.Configuration;
using MultilinerBot.Messages;
using MultilinerBot.Api.Responses;

namespace MultilinerBot
{
    internal static class BuildOperations
    {
        internal class Result
        {
            internal bool AreAllSuccessful = false;
            internal List<string> ErrorMessages = new List<string>();
        }

        internal static Result TryBuildTask(
            IRestApi restApi,
            Branch branch,
            MergeReport mergeReport,
            string taskNumber,
            string[] destinationBranches,
            Dictionary<string, int> branchMergesToObjectsMap,
            string buildStage,
            MultilinerBotConfiguration botConfig)
        {
            Result result = new Result();
            result.AreAllSuccessful = true;

            string targetObjectPrefix = buildStage == BuildProperties.StageValues.PRE_CHECKIN ?
                "sh" : "cs";

            string targetObjectName = buildStage == BuildProperties.StageValues.PRE_CHECKIN ?
                "shelve" : "changeset";

            string ciPlan = buildStage == BuildProperties.StageValues.POST_CHECKIN ?
                botConfig.CI.PlanAfterCheckin : botConfig.CI.PlanBranch;

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Empty;
            BuildProperties properties = null;

            foreach (string destinationBranch in destinationBranches)
            {
                if (!branchMergesToObjectsMap.ContainsKey(destinationBranch))
                    continue;

                scmSpecToSwitchTo = string.Format(
                    "{0}:{1}@{2}",
                    targetObjectPrefix,
                    branchMergesToObjectsMap[destinationBranch],
                    repSpec);

                string comment = string.Format(
                    "Building {0} [{1}], the resulting {0} from merging branch " +
                    "[{2}] to [{3}]",
                    targetObjectName,
                    scmSpecToSwitchTo,
                    branch.FullName,
                    destinationBranch);

                properties = CreateBuildProperties(
                    restApi,
                    taskNumber,
                    branch.FullName,
                    string.Empty,
                    buildStage,
                    destinationBranch,
                    botConfig);

                MultilinerMergebotApi.CI.PlanResult buildResult =
                    MultilinerMergebotApi.CI.Build(
                        restApi,
                        botConfig.CI.Plug,
                        ciPlan,
                        scmSpecToSwitchTo,
                        comment,
                        properties);

                if (buildResult.Succeeded)
                    continue;

                string errorMsg = string.Format(
                    "Build failed. The build plan [{0}] of the resulting {1} [{2}] from merging branch " +
                    "[{3}] to [{4}] has failed. " +
                    "{5}" +
                    "Please check your Continuous Integration report to find out more info about what happened.",
                    ciPlan,
                    targetObjectName,
                    scmSpecToSwitchTo,
                    branch.FullName,
                    destinationBranch,
                    string.IsNullOrWhiteSpace(buildResult.Explanation) ?
                        string.Empty : "Error: [" + buildResult.Explanation +"]");

                result.ErrorMessages.Add(errorMsg);
                result.AreAllSuccessful = false;

                if (buildStage == BuildProperties.StageValues.POST_CHECKIN)
                    continue;

                return result;
            }

            return result;
        }

        static BuildProperties CreateBuildProperties(
            IRestApi restApi,
            string taskNumber,
            string branchName,
            string labelName,
            string buildStagePreCiOrPostCi,
            string destinationBranch,
            MultilinerBotConfiguration botConfig)
        {
            int branchHeadChangesetId = MultilinerMergebotApi.GetBranchHead(
                restApi, botConfig.Repository, branchName);
            ChangesetModel branchHeadChangeset = MultilinerMergebotApi.GetChangeset(
                restApi, botConfig.Repository, branchHeadChangesetId);

            int trunkHeadChangesetId = MultilinerMergebotApi.GetBranchHead(
                restApi, botConfig.Repository, destinationBranch);
            ChangesetModel trunkHeadChangeset = MultilinerMergebotApi.GetChangeset(
                restApi, botConfig.Repository, trunkHeadChangesetId);

            return new BuildProperties
            {
                BranchName = branchName,
                TaskNumber = taskNumber,
                BranchHead = branchHeadChangeset.ChangesetId.ToString(),
                BranchHeadGuid = branchHeadChangeset.Guid.ToString(),
                ChangesetOwner = branchHeadChangeset.Owner,
                TrunkBranchName = destinationBranch,
                TrunkHead = trunkHeadChangeset.ChangesetId.ToString(),
                TrunkHeadGuid = trunkHeadChangeset.Guid.ToString(),
                RepSpec = string.Format("{0}@{1}", botConfig.Repository, botConfig.Server),
                LabelName = labelName,
                Stage = buildStagePreCiOrPostCi
            };
        }

        static readonly ILog mLog = LogManager.GetLogger("BuildOperations");
    }
}
