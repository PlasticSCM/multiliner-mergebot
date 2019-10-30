using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Api.Requests;
using MultilinerBot.Api.Responses;
using MultilinerBot.Messages;

namespace MultilinerBot
{
    internal class MultilinerMergebotApi
    {
        internal static class Attributes
        {
            internal static bool CreateAttribute(
                IRestApi restApi,
                string repoName,
                string attributeName,
                string attributeComment)
            {
                CreateAttributeRequest request = new CreateAttributeRequest()
                {
                    Name = attributeName,
                    Comment = attributeComment
                };

                SingleResponse response = restApi.Attributes.Create(repoName, request);

                return GetBoolValue(response.Value, false);
            }
        }

        internal static class CodeReviews
        {
            internal static void Update(
                IRestApi restApi,
                string repoName,
                string reviewId,
                int newStatus,
                string newTitle)
            {
                UpdateReviewRequest request = new UpdateReviewRequest()
                {
                    Status = newStatus,
                    Title = newTitle
                };

                restApi.CodeReviews.UpdateReview(repoName, reviewId, request);
            }
        }

        internal static class Labels
        {
            internal static void Create(
                IRestApi restApi, string repoName, string labelName, int csetId, string comment)
            {
                CreateLabelRequest request = new CreateLabelRequest()
                {
                    Name = labelName,
                    Changeset = csetId,
                    Comment = comment
                };

                restApi.Labels.Create(repoName, request);
            }
        }

        internal static class Users
        {
            internal static JObject GetUserProfile(IRestApi restApi, string userName)
            {
                return restApi.Users.GetUserProfile(userName);
            }
        }

        internal static class MergeReports
        {
            internal static void ReportMerge(IRestApi restApi, string mergebotName, MergeReport mergeReport)
            {
                restApi.MergeReports.ReportMerge(mergebotName, mergeReport);
            }
        }

        internal class Issues
        {
            internal static bool Connected(
                IRestApi restApi,
                string issueTrackerName)
            {
                SingleResponse response = restApi.Issues.IsConnected(issueTrackerName);
                return GetBoolValue(response.Value, false);
            }

            internal static void SetIssueField(
                IRestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber, string fieldName, string fieldValue)
            {
                SetIssueFieldRequest request = new SetIssueFieldRequest()
                {
                    NewValue = fieldValue
                };

                restApi.Issues.SetIssueField(issueTrackerName,
                    projectKey, taskNumber, fieldName, request);
            }

            internal static string GetIssueUrl(
                IRestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber)
            {
                return restApi.Issues.GetIssueUrl(issueTrackerName,
                    projectKey, taskNumber).Value;
            }

            internal static string GetIssueField(
                IRestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber, string fieldName)
            {
                return restApi.Issues.GetIssueField(issueTrackerName,
                    projectKey, taskNumber, fieldName).Value;
            }
        }

        internal class Notify
        {
            internal static void Message(
                IRestApi restApi,
                string notifierName, string message, List<string> recipients)
            {
                if (recipients == null)
                    return;

                NotifyMessageRequest request = new NotifyMessageRequest()
                {
                    Message = message,
                    Recipients = recipients
                };

                restApi.Notify.NotifyMessage(notifierName, request);
            }
        }

        internal class CI
        {
            internal class PlanResult
            {
                internal bool Succeeded;
                internal string Explanation;
            }

            internal static PlanResult Build(
                IRestApi restApi,
                string ciName,
                string planPath,
                string scmSpecToSwitchTo,
                string comment,
                BuildProperties properties)
            {
                return Run(
                    restApi,
                    ciName,
                    planPath,
                    scmSpecToSwitchTo,
                    comment,
                    ParseBuildProperties.ToDictionary(properties),
                    maxWaitTimeSeconds: 4 * 60 * 60);
            }

            static PlanResult Run(
                IRestApi restApi,
                string ciName,
                string planPath,
                string objectSpec,
                string comment,
                Dictionary<string, string> properties,
                int maxWaitTimeSeconds)
            {
                LaunchPlanRequest request = new LaunchPlanRequest()
                {
                    ObjectSpec = objectSpec,
                    Comment = string.Format("MergeBot - {0}", comment),
                    Properties = properties
                };

                SingleResponse planResponse = restApi.CI.LaunchPlan(
                    ciName, planPath, request);

                GetPlanStatusResponse statusResponse =
                    Task.Run(() =>
                        WaitForFinishedPlanStatus(
                            restApi,
                            ciName, 
                            planResponse.Value,
                            planPath,
                            maxWaitTimeSeconds)
                        ).Result;

                if (statusResponse != null)
                {
                    return new PlanResult()
                    {
                        Succeeded = statusResponse.Succeeded,
                        Explanation = statusResponse.Explanation
                    };
                }

                return new PlanResult()
                {
                    Succeeded = false,
                    Explanation = string.Format(
                        "{0} reached the time limit to get the status " +
                        "for plan:'{1}' and executionId:'{2}'" +
                        "\nRequest details: objectSpec:'{3}' and comment:'{4}'",
                        ciName, planPath, planResponse.Value, objectSpec, comment)
                };
            }

            static async Task<GetPlanStatusResponse> WaitForFinishedPlanStatus(
                IRestApi restApi,
                string ciName,
                string executionId,
                string planPath,
                int maxWaitTimeSeconds)
            {
                long startTime = Environment.TickCount;
                do
                {
                    GetPlanStatusResponse statusResponse = restApi.CI.
                        GetPlanStatus(ciName, executionId, planPath);

                    if (statusResponse.IsFinished)
                        return statusResponse;

                    await Task.Delay(5000);

                } while (Environment.TickCount - startTime < maxWaitTimeSeconds * 1000);

                return null;
            }
        }

        internal static BranchModel GetBranch(
            IRestApi restApi,
            string repoName, string branchName)
        {
            return restApi.GetBranch(repoName, branchName);
        }

        internal static int GetBranchHead(
            IRestApi restApi,
            string repoName, string branchName)
        {
            return restApi.GetBranch(repoName, branchName).HeadChangeset;
        }

        internal static ChangesetModel GetChangeset(
            IRestApi restApi, string repoName, int changesetId)
        {
            return restApi.GetChangeset(repoName, changesetId);
        }

        internal static string GetBranchAttribute(
            IRestApi restApi,
            string repoName, string branchName, string attributeName)
        {
            return restApi.GetAttribute(repoName, attributeName,
                AttributeTargetType.Branch, branchName).Value;
        }

        internal static void ChangeBranchAttribute(
            IRestApi restApi,
            string repoName, string branchName, string attributeName, string attributeValue)
        {
            ChangeAttributeRequest request = new ChangeAttributeRequest()
            {
                TargetType = AttributeTargetType.Branch,
                TargetName = branchName,
                Value = attributeValue
            };

            restApi.ChangeAttribute(repoName, attributeName, request);
        }

        [Flags]
        internal enum MergeToOptions : byte
        {
            None = 0,
            CreateShelve = 1 << 0,
            EnsureNoDstChanges = 1 << 1,
        }

        internal static MergeToResponse MergeBranchTo(
            IRestApi restApi,
            string repoName,
            string sourceBranch,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return MergeTo(
                restApi, repoName, sourceBranch, MergeToSourceType.Branch,
                destinationBranch, comment, options);
        }

        internal static MergeToResponse MergeShelveTo(
            IRestApi restApi,
            string repoName,
            int shelveId,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return MergeTo(
                restApi, repoName, shelveId.ToString(), MergeToSourceType.Shelve,
                destinationBranch, comment, options);
        }

        internal static JArray Find(
            IRestApi restApi,
            string repName,
            string query,
            string queryDateFormat,
            string actionDescription,
            string[] fields)
        {
            return restApi.Find(repName, query, queryDateFormat, actionDescription, fields);
        }

        internal static JArray FindBranchesWithReviews(
            IRestApi restApi,
            string repName,
            string reviewConditions,
            string branchConditions,
            string queryDateFormat,
            string actionDescription,
            string[] fields)
        {
            return restApi.FindBranchesWithReviews(
                repName,
                reviewConditions,
                branchConditions,
                queryDateFormat,
                actionDescription,
                fields);
        }

        internal static void DeleteShelve(
            IRestApi restApi,
            string repoName, int shelveId)
        {
            restApi.DeleteShelve(repoName, shelveId);
        }

        internal static bool IsMergeAllowed(
            IRestApi restApi,
            string repoName,
            string sourceBranchName,
            string destinationBranchName)
        {
            MergeToAllowedResponse response = restApi.IsMergeAllowed(
                repoName, sourceBranchName, destinationBranchName);

            return
                response.Result.Trim().Equals("ok", StringComparison.InvariantCultureIgnoreCase);
        }

        static MergeToResponse MergeTo(
            IRestApi restApi,
            string repoName,
            string source,
            MergeToSourceType sourceType,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            MergeToRequest request = new MergeToRequest()
            {
                SourceType = sourceType,
                Source = source,
                Destination = destinationBranch,
                Comment = comment,
                CreateShelve = options.HasFlag(MergeToOptions.CreateShelve),
                EnsureNoDstChanges = options.HasFlag(MergeToOptions.EnsureNoDstChanges)
            };

            return restApi.MergeTo(repoName, request);
        }

        static bool GetBoolValue(string value, bool defaultValue)
        {
            bool flag;
            if (Boolean.TryParse(value, out flag))
                return flag;

            return defaultValue;
        }
    }
}
