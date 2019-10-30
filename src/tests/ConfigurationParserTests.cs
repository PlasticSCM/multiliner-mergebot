using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;
using MultilinerBot.Configuration;

namespace MultilinerBot.Tests
{
    [TestFixture]
    public class ConfigurationParserTests
    {
        [Test]
        public void Full()
        {
            string configTmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.Full());
                MultilinerBotConfiguration config =
                    MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);

                Assert.That(config.Server, Is.EqualTo("localhost:8084"));
                Assert.That(config.Repository, Is.EqualTo("assets"));
                Assert.That(config.BranchPrefix, Is.EqualTo("AST-"));
                Assert.That(config.MergeToBranchesAttrName, Is.EqualTo("target"));
                Assert.That(config.UserApiKey, Is.EqualTo("BAEE806DB01"));

                Assert.That(config.Plastic, Is.Not.Null);
                Assert.That(config.Plastic.IsApprovedCodeReviewFilterEnabled, Is.False);
                Assert.That(config.Plastic.IsBranchAttrFilterEnabled, Is.True);

                Assert.That(config.Plastic.StatusAttribute, Is.Not.Null);
                Assert.That(config.Plastic.StatusAttribute.Name, Is.EqualTo("status"));
                Assert.That(config.Plastic.StatusAttribute.ResolvedValue, Is.EqualTo("resolved"));
                Assert.That(config.Plastic.StatusAttribute.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Plastic.StatusAttribute.FailedValue, Is.EqualTo("failed"));
                Assert.That(config.Plastic.StatusAttribute.MergedValue, Is.EqualTo("merged"));

                Assert.That(config.Issues, Is.Not.Null);
                Assert.That(config.Issues.Plug, Is.EqualTo("tts"));
                Assert.That(config.Issues.ProjectKey, Is.EqualTo("AST"));
                Assert.That(config.Issues.TitleField, Is.EqualTo("title"));

                Assert.That(config.Issues.StatusField, Is.Not.Null);
                Assert.That(config.Issues.StatusField.Name, Is.EqualTo("status"));
                Assert.That(config.Issues.StatusField.ResolvedValue, Is.EqualTo("validated"));
                Assert.That(config.Issues.StatusField.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Issues.StatusField.FailedValue, Is.EqualTo("open"));
                Assert.That(config.Issues.StatusField.MergedValue, Is.EqualTo("closed"));

                Assert.That(config.CI, Is.Not.Null);
                Assert.That(config.CI.Plug, Is.EqualTo("My Jenkins"));
                Assert.That(config.CI.PlanBranch, Is.EqualTo("debug plan"));
                Assert.That(config.CI.PlanAfterCheckin, Is.EqualTo("release plan"));

                Assert.That(config.Notifiers, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(config.Notifiers[0].Name, Is.EqualTo("notifier1"));
                Assert.That(config.Notifiers[0].Plug, Is.EqualTo("email"));
                Assert.That(config.Notifiers[0].UserProfileField, Is.EqualTo("email"));
                Assert.That(
                    config.Notifiers[0].FixedRecipients,
                    Is.Not.Null.And.Length.EqualTo(2).And.Contains("me").And.Contains("you"));
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void OnlyCIPlug()
        {
            string configTmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIPlug());
                MultilinerBotConfiguration config =
                    MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);

                Assert.That(config.Server, Is.EqualTo("localhost:8084"));
                Assert.That(config.Repository, Is.EqualTo("assets"));
                Assert.That(config.BranchPrefix, Is.EqualTo("AST-"));
                Assert.That(config.MergeToBranchesAttrName, Is.EqualTo("target"));
                Assert.That(config.UserApiKey, Is.EqualTo("BAEE806DB01"));

                Assert.That(config.Plastic, Is.Not.Null);
                Assert.That(config.Plastic.IsApprovedCodeReviewFilterEnabled, Is.False);
                Assert.That(config.Plastic.IsBranchAttrFilterEnabled, Is.True);

                Assert.That(config.Plastic.StatusAttribute, Is.Not.Null);
                Assert.That(config.Plastic.StatusAttribute.Name, Is.EqualTo("status"));
                Assert.That(config.Plastic.StatusAttribute.ResolvedValue, Is.EqualTo("resolved"));
                Assert.That(config.Plastic.StatusAttribute.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Plastic.StatusAttribute.FailedValue, Is.EqualTo("failed"));
                Assert.That(config.Plastic.StatusAttribute.MergedValue, Is.EqualTo("merged"));

                Assert.That(config.Issues, Is.Null);

                Assert.That(config.CI, Is.Not.Null);
                Assert.That(config.CI.Plug, Is.EqualTo("My Jenkins"));
                Assert.That(config.CI.PlanBranch, Is.EqualTo("debug plan"));
                Assert.That(config.CI.PlanAfterCheckin, Is.EqualTo("release plan"));

                Assert.That(config.Notifiers, Is.Not.Null.And.Empty);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void OnlyCIAndNotificationPlugs()
        {
            string configTmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config =
                    MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);

                Assert.That(config.Server, Is.EqualTo("localhost:8084"));
                Assert.That(config.Repository, Is.EqualTo("assets"));
                Assert.That(config.BranchPrefix, Is.EqualTo("AST-"));
                Assert.That(config.MergeToBranchesAttrName, Is.EqualTo("target"));
                Assert.That(config.UserApiKey, Is.EqualTo("BAEE806DB01"));

                Assert.That(config.Plastic, Is.Not.Null);
                Assert.That(config.Plastic.IsApprovedCodeReviewFilterEnabled, Is.False);
                Assert.That(config.Plastic.IsBranchAttrFilterEnabled, Is.True);

                Assert.That(config.Plastic.StatusAttribute, Is.Not.Null);
                Assert.That(config.Plastic.StatusAttribute.Name, Is.EqualTo("status"));
                Assert.That(config.Plastic.StatusAttribute.ResolvedValue, Is.EqualTo("resolved"));
                Assert.That(config.Plastic.StatusAttribute.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Plastic.StatusAttribute.FailedValue, Is.EqualTo("failed"));
                Assert.That(config.Plastic.StatusAttribute.MergedValue, Is.EqualTo("merged"));

                Assert.That(config.Issues, Is.Null);

                Assert.That(config.CI, Is.Not.Null);
                Assert.That(config.CI.Plug, Is.EqualTo("My Jenkins"));
                Assert.That(config.CI.PlanBranch, Is.EqualTo("debug plan"));
                Assert.That(config.CI.PlanAfterCheckin, Is.EqualTo("release plan"));

                Assert.That(config.Notifiers, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(config.Notifiers[0].Name, Is.EqualTo("notifier1"));
                Assert.That(config.Notifiers[0].Plug, Is.EqualTo("email"));
                Assert.That(config.Notifiers[0].UserProfileField, Is.EqualTo("email"));
                Assert.That(config.Notifiers[0].FixedRecipients, Is.Not.Null.And.Empty);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void OnlyCIAndNotificationPlugsNoPostCheckinPlan()
        {
            string configTmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    configTmpFile,
                    BotConfigsForTesting.OnlyCIAndNotificationPlugsNoPostCheckinPlan());
                MultilinerBotConfiguration config =
                    MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);

                Assert.That(config.Server, Is.EqualTo("localhost:8084"));
                Assert.That(config.Repository, Is.EqualTo("assets"));
                Assert.That(config.BranchPrefix, Is.EqualTo("AST-"));
                Assert.That(config.MergeToBranchesAttrName, Is.EqualTo("target"));
                Assert.That(config.UserApiKey, Is.EqualTo("BAEE806DB01"));

                Assert.That(config.Plastic, Is.Not.Null);
                Assert.That(config.Plastic.IsApprovedCodeReviewFilterEnabled, Is.False);
                Assert.That(config.Plastic.IsBranchAttrFilterEnabled, Is.True);

                Assert.That(config.Plastic.StatusAttribute, Is.Not.Null);
                Assert.That(config.Plastic.StatusAttribute.Name, Is.EqualTo("status"));
                Assert.That(config.Plastic.StatusAttribute.ResolvedValue, Is.EqualTo("resolved"));
                Assert.That(config.Plastic.StatusAttribute.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Plastic.StatusAttribute.FailedValue, Is.EqualTo("failed"));
                Assert.That(config.Plastic.StatusAttribute.MergedValue, Is.EqualTo("merged"));

                Assert.That(config.Issues, Is.Null);

                Assert.That(config.CI, Is.Not.Null);
                Assert.That(config.CI.Plug, Is.EqualTo("My Jenkins"));
                Assert.That(config.CI.PlanBranch, Is.EqualTo("debug plan"));
                Assert.That(config.CI.PlanAfterCheckin, Is.Null.Or.Empty);

                Assert.That(config.Notifiers, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(config.Notifiers[0].Name, Is.EqualTo("notifier1"));
                Assert.That(config.Notifiers[0].Plug, Is.EqualTo("email"));
                Assert.That(config.Notifiers[0].UserProfileField, Is.Not.Null.And.Empty);
                Assert.That(
                    config.Notifiers[0].FixedRecipients,
                    Is.Not.Null.And.Length.EqualTo(1).And.Contains("me"));
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void OnlyCIAndTwoNotificationPlugs()
        {
            string configTmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    configTmpFile, BotConfigsForTesting.OnlyCIAndTwoNotificationPlugs());
                MultilinerBotConfiguration config =
                    MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);

                Assert.That(config.Server, Is.EqualTo("localhost:8084"));
                Assert.That(config.Repository, Is.EqualTo("assets"));
                Assert.That(config.BranchPrefix, Is.EqualTo("AST-"));
                Assert.That(config.MergeToBranchesAttrName, Is.EqualTo("target"));
                Assert.That(config.UserApiKey, Is.EqualTo("BAEE806DB01"));

                Assert.That(config.Plastic, Is.Not.Null);
                Assert.That(config.Plastic.IsApprovedCodeReviewFilterEnabled, Is.False);
                Assert.That(config.Plastic.IsBranchAttrFilterEnabled, Is.True);

                Assert.That(config.Plastic.StatusAttribute, Is.Not.Null);
                Assert.That(config.Plastic.StatusAttribute.Name, Is.EqualTo("status"));
                Assert.That(config.Plastic.StatusAttribute.ResolvedValue, Is.EqualTo("resolved"));
                Assert.That(config.Plastic.StatusAttribute.TestingValue, Is.EqualTo("testing"));
                Assert.That(config.Plastic.StatusAttribute.FailedValue, Is.EqualTo("failed"));
                Assert.That(config.Plastic.StatusAttribute.MergedValue, Is.EqualTo("merged"));

                Assert.That(config.Issues, Is.Null);

                Assert.That(config.CI, Is.Not.Null);
                Assert.That(config.CI.Plug, Is.EqualTo("My Jenkins"));
                Assert.That(config.CI.PlanBranch, Is.EqualTo("debug plan"));
                Assert.That(config.CI.PlanAfterCheckin, Is.EqualTo("release plan"));

                Assert.That(config.Notifiers, Is.Not.Null.And.Count.EqualTo(2));
                Assert.That(config.Notifiers[0].Name, Is.EqualTo("notifier1"));
                Assert.That(config.Notifiers[0].Plug, Is.EqualTo("email"));
                Assert.That(config.Notifiers[0].UserProfileField, Is.EqualTo("email"));
                Assert.That(
                    config.Notifiers[0].FixedRecipients,
                    Is.Not.Null.And.Length.EqualTo(2).And.Contains("me").And.Contains("you"));
                Assert.That(config.Notifiers[1].Name, Is.EqualTo("notifier2"));
                Assert.That(config.Notifiers[1].Plug, Is.EqualTo("slack"));
                Assert.That(config.Notifiers[1].UserProfileField, Is.EqualTo("slack"));
                Assert.That(
                    config.Notifiers[1].FixedRecipients,
                    Is.Not.Null.And.Length.EqualTo(2).And.Contains("adam").And.Contains("eve"));
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }
    }
}
