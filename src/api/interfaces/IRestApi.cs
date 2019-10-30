using Newtonsoft.Json.Linq;

using MultilinerBot.Api.Requests;
using MultilinerBot.Api.Responses;

namespace MultilinerBot.Api.Interfaces
{
    public interface IRestApi
    {
        IUsersApi Users { get; }
        IMergeReportsApi MergeReports { get; }
        IIssuesApi Issues { get; }
        INotifyApi Notify { get; }
        ICIApi CI { get; }
        ILabelApi Labels { get; }
        IAttributeApi Attributes { get; }
        ICodeReviewApi CodeReviews { get; }

        BranchModel GetBranch(string repoName, string branchName);

        ChangesetModel GetChangeset(string repoName, int changesetId);

        SingleResponse GetAttribute(
            string repoName,
            string attributeName,
            AttributeTargetType targetType,
            string targetName);

        void ChangeAttribute(string repoName, string attributeName, ChangeAttributeRequest request);

        MergeToResponse MergeTo(string repoName, MergeToRequest request);

        MergeToAllowedResponse IsMergeAllowed(
           string repoName,
           string sourceBranchName,
           string destinationBranchName);

        void DeleteShelve(string repoName, int shelveId);

        JArray Find(
            string repoName,
            string query,
            string queryDateFormat,
            string actionDescription,
            string[] fields);

        JArray FindBranchesWithReviews(
            string repoName,
            string reviewConditions,
            string branchConditions,
            string queryDateFormat,
            string actionDescription,
            string[] fields);
    }

    public interface IUsersApi
    {
        JObject GetUserProfile(string name);
    }

    public interface IMergeReportsApi
    {
        void ReportMerge(string mergebotName, MergeReport mergeReport);
    }

    public interface IIssuesApi
    {
        SingleResponse IsConnected(string issueTrackerName);

        SingleResponse GetIssueUrl(string issueTrackerName, string projectKey, string taskNumber);
        SingleResponse GetIssueField(string issueTrackerName, string projectKey, string taskNumber, string fieldName);
        SingleResponse SetIssueField(
            string issueTrackerName, string projectKey, string taskNumber, string fieldName,
            SetIssueFieldRequest request);
    }

    public interface INotifyApi
    {
        void NotifyMessage(string notifierName, NotifyMessageRequest request);
    }

    public interface ICIApi
    {
        SingleResponse LaunchPlan(string ciName, string planName, LaunchPlanRequest request);
        GetPlanStatusResponse GetPlanStatus(string ciName, string buildId, string planPath);
    }

    public interface ILabelApi
    {
        void Create(string repository, CreateLabelRequest req);
    }

    public interface IAttributeApi
    {
        SingleResponse Create(string repoName, CreateAttributeRequest request);
    }

    public interface ICodeReviewApi
    {
        void UpdateReview(string repoName, string reviewId, UpdateReviewRequest request);
    }
}