using System;

namespace MultilinerBot.Configuration
{
    public class MultilinerBotConfigurationChecker
    {
        public static bool CheckConfiguration(
            MultilinerBotConfiguration botConfig,
            out string errorMessage)
        {
            if (!CheckValidFields(botConfig, out errorMessage))
            {
                errorMessage = string.Format(
                    "multilinerbot can't start without specifying a valid config for the following fields:\n{0}",
                    errorMessage);
                return false;
            }

            return true;
        }

        public static bool CheckValidFields(
            MultilinerBotConfiguration botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Server))
                errorMessage += BuildFieldError("server");

            if (string.IsNullOrEmpty(botConfig.Repository))
                errorMessage += BuildFieldError("repository");

            if (string.IsNullOrEmpty(botConfig.MergeToBranchesAttrName))
                errorMessage += BuildFieldError("attribute name to specify merge destination branches");

            if (string.IsNullOrEmpty(botConfig.UserApiKey))
                errorMessage += BuildFieldError("user api key");

            string propertyErrorMessage = null;
            if (!CheckValidPlasticFields(botConfig.Plastic, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            propertyErrorMessage = null;
            if (!CheckValidIssueTrackerFields(botConfig.Issues, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            propertyErrorMessage = null;
            if (!CheckValidContinuousIntegrationFields(botConfig.CI, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            foreach (MultilinerBotConfiguration.Notifier notifier in botConfig.Notifiers)
            {
                propertyErrorMessage = null;
                if (!CheckValidNotifierFields(notifier, out propertyErrorMessage))
                    errorMessage += propertyErrorMessage;
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        public static bool CheckValidPlasticFields(
            MultilinerBotConfiguration.PlasticSCM botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string fieldNameBeingChecked = "Branch lifecycle";

            if (botConfig == null)
            {
                errorMessage = BuildFieldError(fieldNameBeingChecked);
                return false;
            }

            if (!AreAnyFiltersDefined(botConfig))
            {
                errorMessage = BuildNoFiltersEnabledErrorMessage(fieldNameBeingChecked);
                return false;
            }

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFieldsForPlasticAttr(
                    botConfig.IsApprovedCodeReviewFilterEnabled,
                    botConfig.StatusAttribute,
                    "of the status attribute for Plastic config",
                    out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool AreAnyFiltersDefined(MultilinerBotConfiguration.PlasticSCM botConfig)
        {
            if (botConfig.IsApprovedCodeReviewFilterEnabled)
                return true;

            if (string.IsNullOrWhiteSpace(botConfig.StatusAttribute.Name))
                return false;

            if (string.IsNullOrWhiteSpace(botConfig.StatusAttribute.ResolvedValue))
                return false;

            return true;
        }

        static bool CheckValidIssueTrackerFields(
            MultilinerBotConfiguration.IssueTracker botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildFieldError("plug name for Issue Tracker config");

            if (string.IsNullOrEmpty(botConfig.TitleField))
                errorMessage += BuildFieldError("title field for Issue Tracker config");

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFieldsForIssueTracker(
                    botConfig.StatusField,
                    "of the status field for Issue Tracker config",
                    out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidContinuousIntegrationFields(
            MultilinerBotConfiguration.ContinuousIntegration botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildFieldError("plug name for CI config");

            if (string.IsNullOrEmpty(botConfig.PlanBranch))
                errorMessage += BuildFieldError("plan branch for CI config");

            //botConfig.PlanAfterCheckin could be empty, so we don't check its field.

            return string.IsNullOrEmpty(errorMessage);
        }

        public static bool CheckValidNotifierFields(
            MultilinerBotConfiguration.Notifier notifier,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (notifier == null)
                return true;

            if (string.IsNullOrEmpty(notifier.Plug))
            {
                errorMessage += BuildFieldError(string.Format(
                    "plug name for Notifier '{0}' config", notifier.Name));
            }

            if (IsDestinationInfoEmpty(notifier))
            {
                errorMessage += string.Format("* There is no destination info in the Notifier '{0}'" +
                    " config. Please specify a user profile field, a list of recipients" +
                    " or both (recommended).\n", notifier.Name);
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidStatusPropertyFieldsForIssueTracker(
            MultilinerBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            return CheckValidStatusPropertyFieldsForPlasticAttr(
                false, botConfig, groupNameMessage, out errorMessage);
        }

        static bool CheckValidStatusPropertyFieldsForPlasticAttr(
            bool bIsApprovedCodeReviewFilterEnabled,
            MultilinerBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            return CheckValidStatusPropertyFields(
                bIsApprovedCodeReviewFilterEnabled, botConfig, groupNameMessage, out errorMessage);
        }

        static bool CheckValidStatusPropertyFields(
            bool bIsApprovedCodeReviewFilterEnabled,
            MultilinerBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Name))
                errorMessage += BuildFieldError("name " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.ResolvedValue) && !bIsApprovedCodeReviewFilterEnabled)
                errorMessage += BuildFieldError("resolved value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.FailedValue) && !bIsApprovedCodeReviewFilterEnabled)
                errorMessage += BuildFieldError("failed value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.MergedValue))
                errorMessage += BuildFieldError("merged value " + groupNameMessage);

            if (!string.IsNullOrEmpty(botConfig.ResolvedValue) &&
                !string.IsNullOrEmpty(botConfig.MergedValue) &&
                botConfig.ResolvedValue.Equals(
                    botConfig.MergedValue, StringComparison.InvariantCultureIgnoreCase))
            {
                errorMessage += string.Format(
                    "The 'merged' attribute value: [{0}] must " +
                    "be different than 'resolved' attribute value: [{1}] (case insensitive)\n",
                    botConfig.ResolvedValue, botConfig.MergedValue);
            }

            if (!string.IsNullOrEmpty(botConfig.ResolvedValue) &&
                !string.IsNullOrEmpty(botConfig.FailedValue) &&
                botConfig.ResolvedValue.Equals(
                    botConfig.FailedValue, StringComparison.InvariantCultureIgnoreCase))
            {
                errorMessage += string.Format(
                    "The 'failed' attribute value: [{0}] must " +
                    "be different than 'resolved' attribute value: [{1}] (case insensitive)\n",
                    botConfig.ResolvedValue, botConfig.FailedValue);
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool IsDestinationInfoEmpty(MultilinerBotConfiguration.Notifier botConfig)
        {
            return string.IsNullOrEmpty(botConfig.UserProfileField) &&
                (botConfig.FixedRecipients == null || botConfig.FixedRecipients.Length == 0);
        }

        static string BuildFieldError(string fieldName)
        {
            return string.Format("* The {0} must be defined.\n", fieldName);
        }

        static string BuildNoFiltersEnabledErrorMessage(string fieldName)
        {
            return string.Format(
                "* Either the 'Process reviewed branches only' or the 'Branch lifecycle configuration " +
                "with a status attribute' must be properly enabled in the '{0}' section.", fieldName);
        }
    }
}
