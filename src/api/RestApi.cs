﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;

using log4net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MultilinerBot.Api.Requests;
using MultilinerBot.Api.Responses;
using MultilinerBot.Api.Interfaces;

namespace MultilinerBot.Api
{
    internal class RestApi : IRestApi
    {
        public IUsersApi Users { get; private set; }
        public IMergeReportsApi MergeReports { get; private set; }
        public IIssuesApi Issues { get; private set; }
        public INotifyApi Notify { get; private set; }
        public ICIApi CI { get; private set; }
        public ILabelApi Labels { get; private set; }
        public IAttributeApi Attributes { get; private set; }
        public ICodeReviewApi CodeReviews { get; private set; }

        internal RestApi(string serverUrl, string apiKey)
        {
            mBaseUri = new Uri(serverUrl);
            mApiKey = apiKey;

            Users = new UsersApi(mBaseUri, apiKey);
            MergeReports = new MergeReportsApi(mBaseUri, apiKey);
            Issues = new IssuesApi(mBaseUri, apiKey);
            Notify = new NotifyApi(mBaseUri, apiKey);
            CI = new CIApi(mBaseUri, apiKey);
            Labels = new LabelApi(mBaseUri, apiKey);
            Attributes = new AttributeApi(mBaseUri, apiKey);
            CodeReviews = new CodeReviewApi(mBaseUri, apiKey);
        }

        public BranchModel GetBranch(
            string repoName, string branchName)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetBranch,
                repoName, FormatTargetName(branchName));

            string actionDescription = string.Format(
                "get info of branch br:{0}@{1}", branchName, repoName);
            return Internal.MakeApiRequest<BranchModel>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        public ChangesetModel GetChangeset(string repoName, int changesetId)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetChangeset, repoName, changesetId.ToString());

            string actionDescription = string.Format(
                "get info of changeset cs:{0}@{1}", changesetId, repoName);
            return Internal.MakeApiRequest<ChangesetModel>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        public SingleResponse GetAttribute(
            string repoName,
            string attributeName,
            AttributeTargetType targetType,
            string targetName)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetAttribute,
                repoName, attributeName, targetType.ToString(),
                FormatTargetName(targetName));

            string actionDescription = string.Format(
                "get value of attribute '{0}@{1}' applied to {2} '{3}'",
                attributeName,
                repoName,
                targetType,
                targetName);
            return Internal.MakeApiRequest<SingleResponse>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        public void ChangeAttribute(
            string repoName, string attributeName, ChangeAttributeRequest request)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.ChangeAttribute,
                repoName, attributeName);

            string actionDescription = string.Format(
                "set attribute '{0}@{1}' applied to {2} '{3}' to value '{4}'",
                attributeName,
                repoName,
                request.TargetType,
                request.TargetName,
                request.Value);
            Internal.MakeApiRequest<ChangeAttributeRequest>(
                endpoint, HttpMethod.Put, request, actionDescription, mApiKey);
        }

        public MergeToResponse MergeTo(
            string repoName, MergeToRequest request)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.MergeTo, repoName);

            string actionDescription = string.Format(
                "merge from {0} '{1}' to '{2}'",
                request.SourceType,
                request.Source,
                request.Destination);
            return Internal.MakeApiRequest<MergeToRequest, MergeToResponse>(
                endpoint, HttpMethod.Post, request, actionDescription, mApiKey);
        }

        public MergeToAllowedResponse IsMergeAllowed(
            string repoName, 
            string sourceBranchName, 
            string destinationBranchName)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, 
                ApiEndpoints.IsMergeAllowed, 
                repoName, 
                FormatTargetName(sourceBranchName),
                FormatTargetName(destinationBranchName));

            string actionDescription = string.Format(
                "checking whether is merge allowed on rep {0} from '{1}' to '{2}'",
                repoName,
                sourceBranchName,
                destinationBranchName);

            return Internal.MakeApiRequest<MergeToAllowedResponse>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        public void DeleteShelve(string repoName, int shelveId)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.DeleteShelve,
                repoName, shelveId.ToString());

            string actionDescription = string.Format(
                "delete shelve sh:{0}@{1}", shelveId, repoName);
            Internal.MakeApiRequest(
                endpoint, HttpMethod.Delete, actionDescription, mApiKey);
        }

        public JArray Find(
            string repoName,
            string query,
            string queryDateFormat,
            string actionDescription,
            string[] fields)
        {
            string fieldsQuery = string.Empty;
            if (fields != null && fields.Length > 0)
                fieldsQuery = string.Join(",", fields);

            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.Find, repoName, query, queryDateFormat, fieldsQuery);

            return Internal.MakeApiRequest<JArray>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        public JArray FindBranchesWithReviews(
            string repoName,
            string reviewConditions,
            string branchConditions,
            string queryDateFormat,
            string actionDescription,
            string[] fields)
        {
            string fieldsQuery = string.Empty;
            if (fields != null && fields.Length > 0)
                fieldsQuery = string.Join(",", fields);

            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri,
                ApiEndpoints.FindBranchesWithReviews, 
                repoName,
                reviewConditions,
                branchConditions,
                queryDateFormat,
                fieldsQuery);

            return Internal.MakeApiRequest<JArray>(
                endpoint, HttpMethod.Get, actionDescription, mApiKey);
        }

        static string FormatTargetName(string targetName)
        {
            if (targetName.StartsWith("/"))
                return targetName.Substring(1);

            return targetName;
        }

        static string FormatDate(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-dd hh:mm");
        }

        internal class UsersApi : IUsersApi
        {
            internal UsersApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public JObject GetUserProfile(string name)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Users.GetUserProfile, name);

                string actionDescriptions = string.Format(
                    "get profile of user '{0}'", name);
                return Internal.MakeApiRequest<JObject>(
                    endpoint, HttpMethod.Get, actionDescriptions, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class MergeReportsApi : IMergeReportsApi
        {
            internal MergeReportsApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public void ReportMerge(
                string mergebotName, MergeReport mergeReport)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.MergeReports.ReportMerge,
                    mergebotName);

                string actionDescription = string.Format(
                    "upload merge report of br:{0} (repo ID: {1})",
                    mergeReport.BranchId,
                    mergeReport.RepositoryId);
                Internal.MakeApiRequest<MergeReport>(
                    endpoint, HttpMethod.Put, mergeReport, actionDescription, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class IssuesApi : IIssuesApi
        {
            internal IssuesApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public SingleResponse IsConnected(
                string issueTrackerName)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.IsConnected,
                    issueTrackerName);

                string actionDescription = string.Format(
                    "test connection to '{0}'", issueTrackerName);
                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, actionDescription, mApiKey);
            }

            public SingleResponse GetIssueUrl(
                string issueTrackerName, string projectKey, string taskNumber)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.GetIssueUrl,
                    issueTrackerName, projectKey, taskNumber);

                string actionDescription = string.Format(
                    "get URL of issue {0}-{1} in {2}",
                    projectKey, taskNumber, issueTrackerName);
                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, actionDescription, mApiKey);
            }

            public SingleResponse GetIssueField(
                string issueTrackerName, string projectKey, string taskNumber, string fieldName)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.GetIssueField,
                    issueTrackerName, projectKey, taskNumber, fieldName);

                string actionDescription = string.Format(
                    "get field '{0}' of issue {1}-{2} in {3}",
                    fieldName, projectKey, taskNumber, issueTrackerName);
                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, actionDescription, mApiKey);
            }

            public SingleResponse SetIssueField(
                string issueTrackerName, string projectKey, string taskNumber, string fieldName,
                SetIssueFieldRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.SetIssueField,
                    issueTrackerName, projectKey, taskNumber, fieldName);

                string actionDescription = string.Format(
                    "set field '{0}' of issue {1}-{2} in {3} to value '{4}'",
                    fieldName, projectKey, taskNumber, issueTrackerName, request.NewValue);
                return Internal.MakeApiRequest<SetIssueFieldRequest, SingleResponse>(
                    endpoint, HttpMethod.Put, request, actionDescription, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class NotifyApi : INotifyApi
        {
            internal NotifyApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public void NotifyMessage(
                string notifierName, NotifyMessageRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Notify.NotifyMessage,
                    notifierName);

                string actionDescription = string.Format(
                    "send message to '{0}'", string.Join(", ", request.Recipients));
                Internal.MakeApiRequest<NotifyMessageRequest>(
                    endpoint, HttpMethod.Post, request, actionDescription, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class CIApi : ICIApi
        {
            internal CIApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public SingleResponse LaunchPlan(
                string ciName, string planName, LaunchPlanRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.CI.LaunchPlan,
                    ciName, planName);

                return Internal.MakeApiRequest<LaunchPlanRequest, SingleResponse>(
                    endpoint, HttpMethod.Post, request, "launch CI plan", mApiKey);
            }

            public GetPlanStatusResponse GetPlanStatus(
                string ciName, string buildId, string planPath)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.CI.GetPlanStatus,
                    ciName, buildId, planPath);

                return Internal.MakeApiRequest<GetPlanStatusResponse>(
                    endpoint, HttpMethod.Get, "retrieve CI plan status", mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class AttributeApi : IAttributeApi
        {
            internal AttributeApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public SingleResponse Create(string repoName, CreateAttributeRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.CreateAttribute, repoName);

                string actionDescription = string.Format(
                    "create attribute name {0} on repo {1}", request.Name, repoName);

                return Internal.MakeApiRequest<CreateAttributeRequest, SingleResponse>(
                    endpoint, HttpMethod.Post, request, actionDescription, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class CodeReviewApi : ICodeReviewApi
        {
            internal CodeReviewApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            public void UpdateReview(
                string repoName, string reviewId, UpdateReviewRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, 
                    ApiEndpoints.UpdateReviewStatus,
                    repoName,
                    reviewId);

                string actionDescription = string.Format(
                    "update review id {0} to status {1} and title {2}",
                    reviewId,
                    request.Status,
                    request.Title);

                Internal.MakeApiRequest<UpdateReviewRequest>(
                    endpoint, HttpMethod.Put, request, actionDescription, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class LabelApi : ILabelApi
        {
            internal LabelApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }
            readonly Uri mBaseUri;
            readonly string mApiKey;

            public void Create(string repository, CreateLabelRequest req)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Labels.Create, repository);

                Internal.MakeApiRequest<CreateLabelRequest>(
                    endpoint, HttpMethod.Post, req, "create label " + req.Name, mApiKey);
            }
        }

        static class Internal
        {
            internal static void MakeApiRequest(
                Uri endpoint, HttpMethod httpMethod, string actionDescription, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest(
                        endpoint, httpMethod, apiKey);

                    GetResponse(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(ex, actionDescription, endpoint);
                }
                catch (Exception ex)
                {
                    LogException(
                        actionDescription,
                        ex.Message,
                        ex.StackTrace,
                        endpoint,
                        HttpStatusCode.OK);
                    throw;
                }
            }

            internal static void MakeApiRequest<TReq>(
                Uri endpoint,
                HttpMethod httpMethod,
                TReq body,
                string actionDescription,
                string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest<TReq>(
                        endpoint, httpMethod, body, apiKey);

                    GetResponse(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(ex, actionDescription, endpoint);
                }
                catch (Exception ex)
                {
                    LogException(
                        actionDescription,
                        ex.Message,
                        ex.StackTrace,
                        endpoint,
                        HttpStatusCode.OK);
                    throw;
                }
            }

            internal static TRes MakeApiRequest<TRes>(
                Uri endpoint, HttpMethod httpMethod, string actionDescription, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest(
                        endpoint, httpMethod, apiKey);

                    return GetResponse<TRes>(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(ex, actionDescription, endpoint);
                }
                catch (Exception ex)
                {
                    LogException(
                        actionDescription,
                        ex.Message,
                        ex.StackTrace,
                        endpoint,
                        HttpStatusCode.OK);
                    throw;
                }
            }

            internal static TRes MakeApiRequest<TReq, TRes>(
                Uri endpoint,
                HttpMethod httpMethod,
                TReq body,
                string actionDescription,
                string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest<TReq>(
                        endpoint, httpMethod, body, apiKey);

                    return GetResponse<TRes>(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(ex, actionDescription, endpoint);
                }
                catch (Exception ex)
                {
                    LogException(
                        actionDescription,
                        ex.Message,
                        ex.StackTrace,
                        endpoint,
                        HttpStatusCode.OK);
                    throw;
                }
            }

            static HttpWebRequest CreateWebRequest(
                Uri endpoint, HttpMethod httpMethod, string apiKey)
            {
                HttpWebRequest request = WebRequest.CreateHttp(endpoint);
                request.Method = httpMethod.Method;
                SetApiKeyAuth(request, apiKey);

                request.ContentLength = 0;

                return request;
            }

            static HttpWebRequest CreateWebRequest<TReq>(
                Uri endpoint, HttpMethod httpMethod, TReq body, string apiKey)
            {
                HttpWebRequest request = WebRequest.CreateHttp(endpoint);
                request.Method = httpMethod.Method;
                request.ContentType = "application/json";
                SetApiKeyAuth(request, apiKey);

                WriteBody(request, body);

                return request;
            }

            static void SetApiKeyAuth(HttpWebRequest request, string apiKey)
            {
                request.Headers["Authorization"] = "ApiKey " + apiKey;
            }

            static void WriteBody(WebRequest request, object body)
            {
                using (Stream st = request.GetRequestStream())
                using (StreamWriter writer = new StreamWriter(st))
                {
                    writer.Write(JsonConvert.SerializeObject(body));
                }
            }

            static TRes GetResponse<TRes>(WebRequest request)
            {
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TRes>(reader.ReadToEnd());
                }
            }

            static void GetResponse(WebRequest request)
            {
                using (WebResponse response = request.GetResponse()) ;
            }
        }

        static void LogException(
            string actionDescription,
            string errorMessage,
            string stackTrace,
            Uri endpoint,
            HttpStatusCode statusCode)
        {
            mLog.ErrorFormat(
                "Unable to {0}. The server returned: {1}. {2}",
                actionDescription,
                errorMessage,
                GetStatusCodeDetails(statusCode));

            mLog.DebugFormat("Endpoint URI: {0}", endpoint);
            mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, stackTrace);
        }

        static string GetStatusCodeDetails(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return "Please check that the User API Key assigned to the bot is " +
                        "correct and the associated user has enough permissions.";
                case HttpStatusCode.InternalServerError:
                    return "Please check the Plastic SCM Server log.";
                case HttpStatusCode.NotFound:
                    return "The requested element doesn't exist.";
                case HttpStatusCode.BadRequest:
                    return "The server couldn't understand the request data.";
                default:
                    return string.Empty;
            }
        }

        static class WebServiceException
        {
            internal static Exception AdaptException(
                WebException ex, string actionDescription, Uri endpoint)
            {
                string message = GetExceptionMessage(ex, endpoint);
                LogException(
                    actionDescription,
                    message,
                    ex.StackTrace,
                    endpoint,
                    GetStatusCode(ex.Response));

                return new Exception(message);
            }

            static HttpStatusCode GetStatusCode(WebResponse exceptionResponse)
            {
                HttpWebResponse httpResponse = exceptionResponse as HttpWebResponse;
                return httpResponse != null ? httpResponse.StatusCode : HttpStatusCode.OK;
            }

            static string GetExceptionMessage(WebException ex, Uri endpoint)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response == null)
                    return ex.Message;

                try
                {
                    return ReadErrorMessageFromResponse(response);
                }
                catch (Exception e)
                {
                    mLog.ErrorFormat("Unable to read the error response: {0}", e.Message);
                    mLog.DebugFormat("Endpoint: {0}", endpoint);
                    mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                    return ex.Message;
                }
            }

            static string ReadErrorMessageFromResponse(HttpWebResponse response)
            {
                using (StreamReader resultStream =
                    new StreamReader(response.GetResponseStream()))
                {
                    JObject jObj = JsonConvert.DeserializeObject<JObject>(
                        resultStream.ReadToEnd());

                    return jObj.Value<JObject>("error").Value<string>("message");
                }
            }
        }

        readonly Uri mBaseUri;
        readonly string mApiKey;

        static readonly ILog mLog = LogManager.GetLogger("RestApi");
    }
}
