using System;
using System.Collections.Generic;

using log4net;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Configuration;

namespace MultilinerBot
{
    internal static class ChangeTaskStatus
    {
        internal class Result
        {
            internal bool IsSuccessful = false;
            internal string ErrorMessage = string.Empty;
        }

        internal static Result SetTaskAsTesting(
            IRestApi restApi,
            Branch branch,
            string taskNumber,
            MultilinerBotConfiguration botConfig)
        {
            Result result = new Result();
            result.IsSuccessful = true;

            try
            {
                if (!string.IsNullOrEmpty(botConfig.Plastic.StatusAttribute.TestingValue))
                {
                    MultilinerMergebotApi.ChangeBranchAttribute(
                        restApi, branch.Repository, branch.FullName,
                        botConfig.Plastic.StatusAttribute.Name,
                        botConfig.Plastic.StatusAttribute.TestingValue);
                }

                if (taskNumber != null && botConfig.Issues != null &&
                    !string.IsNullOrEmpty(botConfig.Issues.StatusField.TestingValue))
                {
                    MultilinerMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.TestingValue);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = BuildExceptionErrorMsg(
                   branch.FullName,
                   botConfig.Plastic.StatusAttribute.TestingValue,
                   ex.Message);
            }

            return result;
        }

        internal static Result SetTaskAsFailed(
            IRestApi restApi,
            Branch branch,
            string taskNumber,
            MultilinerBotConfiguration botConfig,
            string codeReviewsStorageFile)
        {
            Result result = new Result();
            result.IsSuccessful = true;

            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                {
                    SetBranchReviewsAsPending(
                        restApi, branch.Repository, branch.Id, codeReviewsStorageFile);
                }

                MultilinerMergebotApi.ChangeBranchAttribute(
                    restApi, branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.FailedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    MultilinerMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.FailedValue);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = BuildExceptionErrorMsg(
                    branch.FullName,
                    botConfig.Plastic.StatusAttribute.FailedValue,
                    ex.Message);
            }

            return result;
        }

        internal static Result SetTaskAsMerged(
            IRestApi restApi,
            Branch branch,
            string taskNumber,
            MultilinerBotConfiguration botConfig,
            string codeReviewsStorageFile)
        {
            Result result = new Result();
            result.IsSuccessful = true;

            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                {
                    ReviewsStorage.DeleteBranchReviews(
                        branch.Repository, branch.Id, codeReviewsStorageFile);
                }

                MultilinerMergebotApi.ChangeBranchAttribute(
                    restApi, branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.MergedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    MultilinerMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.MergedValue);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = BuildExceptionErrorMsg(
                    branch.FullName,
                    botConfig.Plastic.StatusAttribute.MergedValue,
                    ex.Message);
            }

            return result;
        }

        static void SetBranchReviewsAsPending(
            IRestApi restApi,
            string repoName,
            string branchId,
            string codeReviewsStorageFile)
        {
            List<Review> branchReviews = ReviewsStorage.GetBranchReviews(
                repoName, branchId, codeReviewsStorageFile);

            foreach (Review review in branchReviews)
            {
                MultilinerMergebotApi.CodeReviews.Update(
                    restApi,
                    repoName,
                    review.ReviewId,
                    Review.PENDING_STATUS_ID,
                    review.ReviewTitle);
            }
        }

        static string BuildExceptionErrorMsg(string branchFullName, string attValue, string exMessage)
        {
            return string.Format(
               "There was an error setting the branch [{0}] as [{1}]. " +
               "Error: {2}.",
               branchFullName,
               attValue,
               exMessage);
        }

        static readonly ILog mLog = LogManager.GetLogger("ChangeTaskStatus");
    }
}
