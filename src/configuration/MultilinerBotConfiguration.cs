using System;
using System.Collections.Generic;
using System.IO;

using log4net;

using Newtonsoft.Json.Linq;

namespace MultilinerBot.Configuration
{
    public class MultilinerBotConfiguration
    {
        public class PlasticSCM
        {
            public readonly bool IsApprovedCodeReviewFilterEnabled;

            public readonly bool IsBranchAttrFilterEnabled;

            public readonly StatusProperty StatusAttribute;

            public PlasticSCM(
                bool bApprovedCodeReviewFilterEnabled,
                StatusProperty statusAttribute)
            {
                IsApprovedCodeReviewFilterEnabled = bApprovedCodeReviewFilterEnabled;

                StatusAttribute = statusAttribute;

                IsBranchAttrFilterEnabled =
                    statusAttribute != null &&
                    !string.IsNullOrWhiteSpace(statusAttribute.Name) &&
                    !string.IsNullOrWhiteSpace(statusAttribute.ResolvedValue);
            }
        }

        public class IssueTracker
        {
            public readonly string Plug;
            public readonly string ProjectKey;
            public readonly string TitleField;
            public readonly StatusProperty StatusField;

            internal IssueTracker(
                string plug,
                string projectKey,
                string titleField,
                StatusProperty statusField)
            {
                Plug = plug;
                ProjectKey = string.IsNullOrEmpty(projectKey)
                    ? "default_proj"
                    : projectKey;
                TitleField = titleField;
                StatusField = statusField;
            }
        }

        public class ContinuousIntegration
        {
            public readonly string Plug;
            public readonly string PlanBranch;
            public readonly string PlanAfterCheckin;

            internal ContinuousIntegration(string plug, string planBranch, string planAfterCheckin)
            {
                Plug = plug;
                PlanBranch = planBranch;
                PlanAfterCheckin = planAfterCheckin;
            }
        }

        public class Notifier
        {
            public readonly string Name;
            public readonly string Plug;
            public readonly string UserProfileField;
            public readonly string[] FixedRecipients;

            public Notifier(
                string name,
                string plug,
                string userProfileField,
                string[] fixedRecipients)
            {
                Name = name;
                Plug = plug;
                UserProfileField = userProfileField;
                FixedRecipients = fixedRecipients;
            }
        }

        public class StatusProperty
        {
            public readonly string Name;
            public readonly string ResolvedValue;
            public readonly string TestingValue;
            public readonly string FailedValue;
            public readonly string MergedValue;

            public StatusProperty(
                string name,
                string resolvedValue,
                string testingValue,
                string failedValue,
                string mergedValue)
            {
                Name = name;
                ResolvedValue = resolvedValue;
                TestingValue = testingValue;
                FailedValue = failedValue;
                MergedValue = mergedValue;
            }
        }

        public readonly string Server;
        public readonly string Repository;
        public readonly string MergeToBranchesAttrName;
        public readonly string BranchPrefix;
        public readonly string UserApiKey;
        public readonly PlasticSCM Plastic;
        public readonly IssueTracker Issues;
        public readonly ContinuousIntegration CI;
        public readonly List<Notifier> Notifiers;

        public static MultilinerBotConfiguration BuidFromConfigFile(string configFile)
        {
            try
            {
                string fileContent = File.ReadAllText(configFile);
                JObject jsonObject = JObject.Parse(fileContent);

                if (jsonObject == null)
                    return null;

                return new MultilinerBotConfiguration(
                    GetPropertyValue(jsonObject, "server"),
                    GetPropertyValue(jsonObject, "repository"),
                    GetPropertyValue(jsonObject, "merge_to_branches_attr_name"),
                    GetPropertyValue(jsonObject, "branch_prefix"),
                    GetPropertyValue(jsonObject, "bot_user"),
                    BuildPlasticSCM(jsonObject["plastic_group"]),
                    BuildIssueTracker(jsonObject["issues_group"]),
                    BuildContinuousIntegration(jsonObject["ci_group"]),
                    BuildNotifiers(jsonObject["notifier_group"] as JObject));
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Multilinerbot configuration cannot be read from '{0}' : {1}",
                    configFile, ex.Message);
                mLog.DebugFormat("StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }

            return null;
        }

        static PlasticSCM BuildPlasticSCM(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            return new PlasticSCM(
                GetBoolValue(jsonToken["code_review_group"], "is_enabled", false),
                BuildStatusProperty(jsonToken["status_attribute_group"]));
        }

        static IssueTracker BuildIssueTracker(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            string plug = GetPropertyValue(jsonToken, "plug");

            if (string.IsNullOrEmpty(plug))
                return null;

            return new IssueTracker(
                plug,
                GetPropertyValue(jsonToken, "project_key"),
                GetPropertyValue(jsonToken, "title_field"),
                BuildStatusProperty(jsonToken["status_field_group"]));
        }

        static ContinuousIntegration BuildContinuousIntegration(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            string plug = GetPropertyValue(jsonToken, "plug");

            if (string.IsNullOrEmpty(plug))
                return null;

            return new ContinuousIntegration(
                plug,
                GetPropertyValue(jsonToken, "plan"),
                GetPropertyValue(jsonToken, "planAfterCheckin"));
        }

        static List<Notifier> BuildNotifiers(JObject jsonToken)
        {
            if (jsonToken == null)
                return null;

            List<Notifier> result = new List<Notifier>();
            foreach (KeyValuePair<string, JToken> notifierEntry in jsonToken)
            {
                string plug = GetPropertyValue(notifierEntry.Value, "plug");

                if (string.IsNullOrEmpty(plug))
                    continue;

                result.Add(new Notifier(
                    notifierEntry.Key,
                    plug,
                    GetPropertyValue(notifierEntry.Value, "user_profile_field"),
                    GetFixedRecipientsArray(GetPropertyValue(
                        notifierEntry.Value, "fixed_recipients"))));
            }
            return result;
        }

        static StatusProperty BuildStatusProperty(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            return new StatusProperty(
                GetPropertyValue(jsonToken, "name"),
                GetPropertyValue(jsonToken, "resolved_value"),
                GetPropertyValue(jsonToken, "testing_value"),
                GetPropertyValue(jsonToken, "failed_value"),
                GetPropertyValue(jsonToken, "merged_value"));
        }

        static string[] GetFixedRecipientsArray(string fixedRecipients)
        {
            if (fixedRecipients == null)
                return null;

            string[] result = fixedRecipients.Split(
                new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        static string GetPropertyValue(JToken jsonToken, string key)
        {
            if (jsonToken == null)
                return null;

            JToken jsonProperty = jsonToken[key];

            if (jsonProperty == null)
                return null;

            return jsonProperty.Value<string>();
        }

        static bool GetBoolValue(JToken jsonToken, string key, bool defaultValue)
        {
            if (jsonToken == null)
                return defaultValue;

            JToken jsonProperty = jsonToken[key];

            if (jsonProperty == null)
                return defaultValue;

            bool fieldValue = false;

            if (jsonProperty.Type == JTokenType.Boolean)
            {
                fieldValue = jsonProperty.Value<bool>();
                return fieldValue;
            }

            if (jsonProperty.Type != JTokenType.String)
                throw new NotSupportedException(
                    string.Format("Value {0} is not supported", jsonProperty.ToString()));

            string valueStr = jsonProperty.Value<string>();
            if ("yes".Equals(valueStr, StringComparison.OrdinalIgnoreCase))
            {
                fieldValue = true;
                return fieldValue;
            }

            if ("no".Equals(valueStr, StringComparison.OrdinalIgnoreCase))
            {
                fieldValue = false;
                return false;
            }

            throw new NotSupportedException(
                string.Format("Value {0} is not supported", valueStr));
        }

        MultilinerBotConfiguration(
            string server,
            string repository,
            string mergeToBranchesAttrName,
            string branchPrefix,
            string userApiKey,
            PlasticSCM plastic,
            IssueTracker issues,
            ContinuousIntegration ci,
            List<Notifier> notifiers)
        {
            Server = server;
            Repository = repository;
            MergeToBranchesAttrName = mergeToBranchesAttrName;
            BranchPrefix = branchPrefix;
            UserApiKey = userApiKey;
            Plastic = plastic;
            Issues = issues;
            CI = ci;
            Notifiers = notifiers;
        }

        static readonly ILog mLog = LogManager.GetLogger("MultilinerBotConfiguration");
    }
}
