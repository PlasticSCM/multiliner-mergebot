using System;
using System.Collections.Generic;

using NUnit.Framework;
using MultilinerBot.Configuration;

namespace MultilinerBot.Tests
{
    [TestFixture]
    public class ConfigurationCheckerTests
    {
        [Test]
        public void TestNoAttrNoCodeReviewFiltersDefinedFails()
        {
            bool bIsCodeReviewFilterEnabled = false;

            string attrName = string.Empty;
            string resolvedAttrValue = string.Empty;
            string testingAttrValue = string.Empty;
            string failedAttrValue = string.Empty;
            string mergedAttrValue = string.Empty;

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsFalse(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsFalse(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsFalse(bResult, "CheckValidPlasticFields should have failed due to no filters defined");

            Assert.AreEqual(Normalize(BuildNoFiltersEnabledErrorMessage()), Normalize(errorMsg));
        }

        [Test]
        public void TestNoValidAttrYesCodeReviewFilterDefinedFails()
        {
            bool bIsCodeReviewFilterEnabled = true;

            string attrName = string.Empty;
            string resolvedAttrValue = string.Empty;
            string testingAttrValue = string.Empty;
            string failedAttrValue = string.Empty;
            string mergedAttrValue = string.Empty;

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsTrue(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsFalse(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsFalse(
                bResult, 
                "CheckValidPlasticFields should have failed " +
                "due to no valid basic attr name and merged value defined when only code review filter enabled");

            Assert.AreEqual(Normalize(BuildInvalidAttributeDefinedMessage()), Normalize(errorMsg));
        }

        [Test]
        public void TestNoAttrYesCodeReviewFilterDefinedSucceeds()
        {

            bool bIsCodeReviewFilterEnabled = true;

            string attrName = "status";
            string resolvedAttrValue = string.Empty;
            string testingAttrValue = string.Empty;
            string failedAttrValue = string.Empty;
            string mergedAttrValue = "merged";

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsTrue(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsFalse(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsTrue(bResult, "CheckValidPlasticFields should have succeed!");

            Assert.AreEqual(string.Empty, errorMsg);
        }

        [Test]
        public void TestNoValidAttrNoCodeReviewFilterDefinedFails()
        {

            bool bIsCodeReviewFilterEnabled = false;

            string attrName = "status";
            string resolvedAttrValue = string.Empty;
            string testingAttrValue = "testing";
            string failedAttrValue = "failed";
            string mergedAttrValue = "merged";

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsFalse(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsFalse(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsFalse(
                bResult,
                "CheckValidPlasticFields should have failed due to no " +
                "resolved value set when only attr filter is defined");

            Assert.AreEqual(
                Normalize(BuildNoFiltersEnabledErrorMessage()), Normalize(errorMsg));
        }

        [Test]
        public void TestNoValidMergedAttrNoCodeReviewFilterDefinedFails()
        {

            bool bIsCodeReviewFilterEnabled = false;

            string attrName = "status";
            string resolvedAttrValue = "resolved";
            string testingAttrValue = "testing";
            string failedAttrValue = "failed";
            string mergedAttrValue = string.Empty;

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsFalse(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsTrue(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsFalse(
                bResult,
                "CheckValidPlasticFields should have failed due to no " +
                "resolved value set when only attr filter is defined");

            Assert.AreEqual(
                Normalize("* The merged value of the status attribute for Plastic config must be defined."), 
                Normalize(errorMsg));
        }

        [Test]
        public void TestNoValidFailedAttrNoCodeReviewFilterDefinedFails()
        {

            bool bIsCodeReviewFilterEnabled = false;

            string attrName = "status";
            string resolvedAttrValue = "resolved";
            string testingAttrValue = "testing";
            string failedAttrValue = string.Empty;
            string mergedAttrValue = "merged";

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsFalse(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsTrue(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsFalse(
                bResult,
                "CheckValidPlasticFields should have failed due to no " +
                "resolved value set when only attr filter is defined");

            Assert.AreEqual(
                Normalize("* The failed value of the status attribute for Plastic config must be defined."),
                Normalize(errorMsg));
        }

        [Test]
        public void TestValidAttrNoCodeReviewFilterDefinedSuccess()
        {

            bool bIsCodeReviewFilterEnabled = false;

            string attrName = "status";
            string resolvedAttrValue = "resolved";
            string testingAttrValue = string.Empty;
            string failedAttrValue = "failed";
            string mergedAttrValue = "merged";

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            Assert.IsFalse(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsTrue(plasticConfig.IsBranchAttrFilterEnabled);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsTrue(bResult, "CheckValidPlasticFields should have succeed!");
            Assert.AreEqual(string.Empty, errorMsg);
        }

        [Test]
        public void TestNoValidMergedAttrYesCodeReviewFilterDefinedFails()
        {

            bool bIsCodeReviewFilterEnabled = true;

            string attrName = "status";
            string resolvedAttrValue = "resolved";
            string testingAttrValue = "testing";
            string failedAttrValue = "failed";
            string mergedAttrValue = string.Empty;

            string errorMsg;

            MultilinerBotConfiguration.StatusProperty statusConf =
                new MultilinerBotConfiguration.StatusProperty(
                    attrName, resolvedAttrValue, testingAttrValue, failedAttrValue, mergedAttrValue);

            MultilinerBotConfiguration.PlasticSCM plasticConfig = new MultilinerBotConfiguration.PlasticSCM(
                bIsCodeReviewFilterEnabled,
                statusConf);

            bool bResult = MultilinerBotConfigurationChecker.CheckValidPlasticFields(plasticConfig, out errorMsg);

            Assert.IsTrue(plasticConfig.IsApprovedCodeReviewFilterEnabled);
            Assert.IsTrue(plasticConfig.IsBranchAttrFilterEnabled);

            Assert.IsFalse(
                bResult,
                "CheckValidPlasticFields should have failed due to no " +
                "resolved value set when only attr filter is defined");

            Assert.AreEqual(
                Normalize("* The merged value of the status attribute for Plastic config must be defined."),
                Normalize(errorMsg));
        }

        [Test]
        public void TestInvalidNotifierNoPlug()
        {
            MultilinerBotConfiguration.Notifier notifier = 
                new MultilinerBotConfiguration.Notifier(
                    "notifier1",
                    string.Empty,
                    "profileFields1",
                    new string[]
                    {
                        "fixedField1"
                    });

            string errorMsg;
            bool bResult = MultilinerBotConfigurationChecker.CheckValidNotifierFields(
                notifier, out errorMsg);

            string expectedError = "plug name for Notifier 'notifier1' config";
            Assert.That(bResult, Is.False, "CheckValidNotifierFields should have failed!");
            Assert.That(errorMsg, Is.Not.Null.Or.Empty.And.EqualTo(expectedError));
        }

        [Test]
        public void TestInvalidNotifierNoDestination()
        {
            MultilinerBotConfiguration.Notifier notifier =
                new MultilinerBotConfiguration.Notifier(
                    "notifier1", "plug1", string.Empty, new string[0]);

            string errorMsg;
            bool result = MultilinerBotConfigurationChecker.CheckValidNotifierFields(
                notifier, out errorMsg);

            string expectedError = "*There is no destination info in the Notifier 'notifier1'" +
                " config. Please specify a user profile field, a list of recipients" +
                " or both (recommended).\n";

            Assert.That(result, Is.False, "CheckValidNotifierFields should have failed!");
            Assert.That(errorMsg, Is.Not.Null.Or.Empty.And.EqualTo(expectedError));
        }

        [Test]
        public void TestValidNotifiersNoFixedRecipients()
        {
            MultilinerBotConfiguration.Notifier notifier =
                new MultilinerBotConfiguration.Notifier(
                    "notifier1", "plug1", "profileFields1", null);

            string errorMsg;
            bool bResult = MultilinerBotConfigurationChecker.CheckValidNotifierFields(
                notifier, out errorMsg);

            Assert.That(bResult, Is.True, "CheckValidNotifierFields shouldn't have failed!");
            Assert.That(errorMsg, Is.Null.Or.Empty);
        }

        [Test]
        public void TestValidNotifiersNoProfileField()
        {
            MultilinerBotConfiguration.Notifier notifier =
                new MultilinerBotConfiguration.Notifier(
                    "notifier1", "plug1", null, new string[] { "recipient" });

            string errorMsg;
            bool bResult = MultilinerBotConfigurationChecker.CheckValidNotifierFields(
                notifier, out errorMsg);

            Assert.That(bResult, Is.True, "CheckValidNotifierFields shouldn't have failed!");
            Assert.That(errorMsg, Is.Null.Or.Empty);
        }

        static string BuildNoFiltersEnabledErrorMessage()
        {
            return
                "* Either the 'Process reviewed branches only' or the 'Branch lifecycle configuration " +
                "with a status attribute' must be properly enabled in the 'Branch lifecycle' section.";
        }

        static string BuildInvalidAttributeDefinedMessage()
        {
            return
                "* The name of the status attribute for Plastic config must be defined." + Environment.NewLine +
                "* The merged value of the status attribute for Plastic config must be defined.";
        }

        static string Normalize(string message)
        {
            return message.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }
    }
}
