using log4net;
using System;
using System.Collections.Generic;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Configuration;

namespace MultilinerBot
{
    internal static class Notifier
    {
        internal static void NotifyTaskStatus(
            IRestApi restApi,
            string owner,
            string message,
            List<MultilinerBotConfiguration.Notifier> notifiers)
        {
            if (notifiers == null || notifiers.Count == 0)
                return;

            foreach (MultilinerBotConfiguration.Notifier notifier in notifiers)
            {
                try
                {
                    List<string> recipients = GetNotificationRecipients(
                        restApi, owner, notifier);

                    MultilinerMergebotApi.Notify.Message(
                        restApi, notifier.Plug, message, recipients);
                }
                catch (Exception e)
                {
                    mLog.ErrorFormat("Error notifying task status message '{0}', notifier '{1}': {2}",
                        message, notifier.Name, e.Message);
                    mLog.DebugFormat("StackTrace:{0}{1}", Environment.NewLine, e.StackTrace);
                }
            }
        }

        static List<string> GetNotificationRecipients(
            IRestApi restApi,
            string owner,
            MultilinerBotConfiguration.Notifier notificationsConfig)
        {
            List<string> recipients = new List<string>();
            recipients.Add(owner);

            if (notificationsConfig.FixedRecipients != null)
                recipients.AddRange(notificationsConfig.FixedRecipients);

            return ResolveUserProfile.ResolveFieldForUsers(
                restApi, recipients, notificationsConfig.UserProfileField);
        }

        static readonly ILog mLog = LogManager.GetLogger("Notifier");
    }
}
