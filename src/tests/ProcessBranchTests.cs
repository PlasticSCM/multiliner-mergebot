using System.IO;

using NUnit.Framework;
using Moq;

using MultilinerBot.Api.Interfaces;
using MultilinerBot.Api.Requests;
using MultilinerBot.Configuration;

namespace MultilinerBot.Tests
{
    [TestFixture]
    public class ProcessBranchTests
    {
        [Test]
        public void SetFailedAndStopProcessingWhenNoAttrSetInBranch()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndTwoNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "1", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = ""});

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                Assert.AreEqual(ProcessBranch.Result.Failed, result);

                apiMock.RestApi.Verify(
                    mock => mock.GetAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                      It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                      It.Is<string>(branchName => branchName.Equals(branchToTest.FullName))),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.GetBranch(
                      It.IsAny<string>(),
                      It.IsAny<string>()), Times.Never());

                string message = string.Format(
                    "The attribute [{0}] of branch [{1}",
                    config.MergeToBranchesAttrName,
                    branchToTest.FullName);
                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.Is<string>(name => name == "slack"),
                      It.Is<NotifyMessageRequest>(req => req.Message.StartsWith(message))), 
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.Is<string>(name => name == "email"),
                      It.Is<NotifyMessageRequest>(req => req.Message.StartsWith(message))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.IsAny<string>(),
                        It.IsAny<int>()),
                    Times.Never());
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void SetFailedAndStopProcessingWhenTargetBranchDoesntExist()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndTwoNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");
                Branch mainBranch = new Branch(config.Repository, "1", "main", "main","");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, dontexist" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("dontexist")))).Returns(new Api.Responses.BranchModel());

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                Assert.AreEqual(ProcessBranch.Result.Failed, result);

                apiMock.RestApi.Verify(
                    mock => mock.GetAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                      It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                      It.Is<string>(branchName => branchName.Equals(branchToTest.FullName))),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.GetBranch(
                      It.IsAny<string>(),
                      It.IsAny<string>()), Times.Exactly(2));

                string message = string.Format(
                    "The destination branch [{0}@{1}@{2}] specified in attribute [{3}] "
                    + "of branch [{4}@{1}@{2}] does not exist. ",
                    "dontexist",
                    config.Repository,
                    config.Server,
                    config.MergeToBranchesAttrName,
                    branchToTest.FullName);
                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.Is<string>(name => name == "slack"),
                      It.Is<NotifyMessageRequest>(req => req.Message.StartsWith(message))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.Is<string>(name => name == "email"),
                      It.Is<NotifyMessageRequest>(req => req.Message.StartsWith(message))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.IsAny<string>(),
                        It.IsAny<int>()),
                    Times.Never());
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void CheckNoDuplicatedBuildsSameDstBranch()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");
                Branch mainBranch = new Branch(config.Repository, "1", "main", "main", "");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master, main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master" });

                ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.GetAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                      It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                      It.Is<string>(branchName => branchName.Equals(branchToTest.FullName))),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.GetBranch(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(branch => branch.Equals("master"))), Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.GetBranch(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(branch => branch.Equals("main"))), Times.Exactly(1));
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void ShelvesCreationNoMergesNeededSetAsMerged()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.IsAny<MergeToRequest>())).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.MergeNotNeeded });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                Assert.AreEqual(ProcessBranch.Result.Failed, result);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Exactly(1));

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Branch [main/AST-001] was already merged to [main] (No merge needed).") &&
                        req.Message.Contains(
                            "Branch [main/AST-001] was already merged to [master] (No merge needed)."))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.IsAny<string>(),
                        It.IsAny<int>()),
                    Times.Never());
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void ShelvesCreationAMergeFailedConflicts()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master, fix" });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("fix")))).Returns(new Api.Responses.BranchModel() { Name = "fix" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = -99 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = -98 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("fix")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.Conflicts, Message = "merge_conflicts" });

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                Assert.AreEqual(ProcessBranch.Result.Failed, result);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Equals(
                            "Can't merge branch [main/AST-001] to [fix]. Reason: merge_conflicts."))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.IsAny<string>(),
                        It.IsAny<int>()),
                    Times.Exactly(2));
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void ShelvesCreationMixedMergeFailedConflictsAndMergeNotNeeded()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master, fix" });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("fix")))).Returns(new Api.Responses.BranchModel() { Name = "fix" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.MergeNotNeeded });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = -98 });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("fix")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.Conflicts, Message = "merge_conflicts" });

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                Assert.AreEqual(ProcessBranch.Result.Failed, result);

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(3));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(0));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Branch [main/AST-001] was already merged to [main] (No merge needed).") &&
                        req.Message.Contains(
                            "Can't merge branch [main/AST-001] to [fix]. Reason: merge_conflicts."))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.IsAny<string>(),
                        It.IsAny<int>()),
                    Times.Once());
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void ShelvesCreationAMergeIsNotNeededTheOtherContinues()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugsNoPostCheckinPlan());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int shelveId = -98;
                int csetId = 98;

                apiMock.RestApi.
                    Setup(mock => mock.GetChangeset(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).
                    Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset});

                apiMock.RestApi.
                    Setup(mock => mock.GetChangeset(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).
                    Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main")))).Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.MergeNotNeeded });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") && 
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = csetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") && 
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = shelveId });

                string ciBuildId = "77";
                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.IsAny<LaunchPlanRequest>())).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Exactly(1));

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Branch [main/AST-001] is already merged to some of the specified destination branches in the attribute [target]. The testBot mergebot will continue building the merge(s) from branch [main/AST-001] to [master].") &&
                        req.Message.Contains(
                            "Branch [main/AST-001] was already merged to [main] (No merge needed)."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged to [master]"))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(1));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + shelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(shelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Ok, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void ShelvesCreationAllMergeShelvesOK()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugsNoPostCheckinPlan());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainCsetId = 888;
                int mergeToMainShelveId = -888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Exactly(1));

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Branch [main/AST-001] is already merged"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged to [main, master]"))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Ok, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void PreCheckinPlanFailsOnFirstShelveNoMergesPerformed()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugsNoPostCheckinPlan());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainCsetId = 888;
                int mergeToMainShelveId = -888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = false });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build failed. The build plan [debug plan] of the resulting shelve [sh:-888@assets@localhost:8084] " +
                            "from merging branch [main/AST-001] to [main] has failed. Please check your Continuous Integration " +
                            "report to find out more info about what happened."))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(0));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Never());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Failed, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void PreCheckinPlanFailsOnSecondShelveNoMergesPerformed()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugsNoPostCheckinPlan());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainCsetId = 888;
                int mergeToMainShelveId = -888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = false });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Exactly(1));

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build failed. The build plan [debug plan] of the resulting shelve [sh:-777@assets@localhost:8084] " +
                            "from merging branch [main/AST-001] to [master] has failed. Please check your Continuous Integration " +
                            "report to find out more info about what happened."))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(0));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Failed, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void CheckinShelveFailsOnOneOfThem()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainShelveId = -888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                string ciBuildId2Post = "670";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.MultipleHeads, Message = "multiple_heads_message" });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2Post });                

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2Post)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build successful after merging branch [main/AST-001] to the following destination branches: [master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Failed build. The result of building merges from branch [main/AST-001] to [main, master] " +
                            "went OK, but there were some errors checking-in the resulting shelves") &&
                        req.Message.Contains("Can't checkin shelve ["+ mergeToMainShelveId + "], the resulting shelve from merging branch [main/AST-001] to [main]. Reason: multiple_heads_message"))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged"))),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                //post ci plan just called once
                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.IsAny<string>(),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Failed, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void CheckinShelveButOnOneOfThemFailsDueNewDstChanges()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainShelveId = -888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                string ciBuildId2Post = "670";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.DestinationChanges, Message = "new_dst_changes_message" });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2Post });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2Post)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.ResolvedValue)),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build successful after merging branch [main/AST-001] to the following destination branches: [master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] after being merged in the following destination branches: [master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Branch [main/AST-001] will be enqueued again, as new changesets appeared in merge " +
                            "destination branches, and thus, the branch needs to be tested again to include those " +
                            "new changesets in the merge.") &&
                        req.Message.Contains("new changesets appeared in destination branch while mergebot testBot was processing the merge from [main/AST-001] to [main]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged"))),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                //post ci plan just called once
                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.IsAny<string>(),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.NotReady, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void CheckinShelveOkButTheBuildPlanOfFirstCheckinFails()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainShelveId = -888;
                int mergeToMainCsetId = 888;

                int mergeToMasterCsetId = 777;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                string ciBuildId1Post = "660";
                string ciBuildId2Post = "670";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainCsetId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterCsetId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMainCsetId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1Post });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2Post });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1Post)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = false });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2Post)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build successful after merging branch [main/AST-001] to the following destination branches: [main, master]."))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] after being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build failed. The build plan [release plan] of the resulting changeset [cs:888@assets@localhost:8084] " + 
                            "from merging branch [main/AST-001] to [main] has failed. Please check your Continuous Integration " +
                            "report to find out more info about what happened."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Failed build. The result of building merges from branch [main/AST-001]"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged to [main, master]"))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.
                   Verify(mock => mock.GetPlanStatus(
                       It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                       It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                       It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                   Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMasterCsetId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("cs:" + mergeToMainCsetId))),
                    Times.Once());

                //post ci plan called for all successful checkins
                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.IsAny<string>(),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin))),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Failed, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }

        [Test]
        public void CheckinShelveFailsAllOfThemDueToAnyReasonNoPostCIBuildsTriggered()
        {
            string configTmpFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(configTmpFile, BotConfigsForTesting.OnlyCIAndNotificationPlugs());
                MultilinerBotConfiguration config = MultilinerBotConfiguration.BuidFromConfigFile(configTmpFile);
                Branch branchToTest = new Branch(config.Repository, "2", "main/AST-001", "pixi", "branch comment");

                RestApiMock apiMock = new RestApiMock();

                apiMock.RestApi.Setup(mock => mock.GetAttribute(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(attr => attr.Equals(config.MergeToBranchesAttrName)),
                    It.Is<AttributeTargetType>(type => type == AttributeTargetType.Branch),
                    It.Is<string>(branchName => branchName.Equals(branchToTest.FullName)))).Returns(
                    new Api.Responses.SingleResponse() { Value = "main, master" });

                int mainHeadCset = 44;
                int masterHeadCset = 4;
                int branchToTestHeadCset = 55;

                int mergeToMainShelveId = -888;
                int mergeToMasterShelveId = -777;

                string ciBuildId1 = "66";
                string ciBuildId2 = "67";

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(mainCsetId => mainCsetId.Equals(mainHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                     It.Is<string>(repo => repo.Equals(config.Repository)),
                     It.Is<int>(masterCsetId => masterCsetId.Equals(masterHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetChangeset(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<int>(branchToTestCsetId => branchToTestCsetId.Equals(branchToTestHeadCset)))).Returns(new Api.Responses.ChangesetModel() { ChangesetId = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.IsMergeAllowed(
                     It.IsAny<string>(),
                     It.IsAny<string>(),
                     It.IsAny<string>())).Returns(new Api.Responses.MergeToAllowedResponse() { Result = "ok" });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("main")))).Returns(new Api.Responses.BranchModel() { Name = "main", HeadChangeset = mainHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals("master")))).Returns(new Api.Responses.BranchModel() { Name = "master", HeadChangeset = masterHeadCset });

                apiMock.RestApi.Setup(mock => mock.GetBranch(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<string>(branch => branch.Equals(branchToTest.FullName)))).Returns(new Api.Responses.BranchModel() { Name = branchToTest.FullName, RepositoryId = "repId", Id = 9, HeadChangeset = branchToTestHeadCset });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMainShelveId });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(true)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.OK, ChangesetNumber = mergeToMasterShelveId });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId1 });

                apiMock.CiApi.Setup(mock => mock.LaunchPlan(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                    It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId)))).Returns(new Api.Responses.SingleResponse() { Value = ciBuildId2 });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.CiApi.Setup(mock => mock.GetPlanStatus(
                    It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                    It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                    It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)))).Returns(new Api.Responses.GetPlanStatusResponse() { IsFinished = true, Succeeded = true });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("main") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.MultipleHeads, Message = "multiple_heads_message" });

                apiMock.RestApi.Setup(mock => mock.MergeTo(
                    It.Is<string>(repo => repo.Equals(config.Repository)),
                    It.Is<MergeToRequest>(req => req.Destination.Equals("master") &&
                                          req.CreateShelve.Equals(false)))).
                    Returns(new Api.Responses.MergeToResponse() { Status = Api.Responses.MergeToResultStatus.DestinationChanges, Message = "new_dst_changes_message" });

                apiMock.RestApi.Setup(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.IsAny<int>()));

                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    apiMock.RestApi.Object,
                    branchToTest,
                    config,
                    "testBot",
                    string.Empty);

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.TestingValue)),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.MergedValue)),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.ChangeAttribute(
                      It.Is<string>(repo => repo.Equals(config.Repository)),
                      It.Is<string>(attr => attr.Equals(config.Plastic.StatusAttribute.Name)),
                      It.Is<ChangeAttributeRequest>(req => req.Value == config.Plastic.StatusAttribute.FailedValue)),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] before being merged in the following destination branches: [main, master]."))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Build successful after merging branch [main/AST-001]"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Testing branch [main/AST-001] after being merged in the following destination branches:"))),
                    Times.Never());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "Failed build. The result of building merges from branch [main/AST-001] to [main, master] " +
                            "went OK, but there were some errors checking-in the resulting shelves") &&
                        req.Message.Contains("Can't checkin shelve [" + mergeToMainShelveId + "], the resulting shelve " +
                        "from merging branch [main/AST-001] to [main]. Reason: multiple_heads_message") &&
                        req.Message.Contains("Can't checkin shelve ["+ mergeToMasterShelveId +"], the resulting shelve " +
                        "from merging branch [main/AST-001] to [master]. Reason: new changesets appeared in destination branch"))),
                    Times.Once());

                apiMock.NotifyApi.Verify(
                    mock => mock.NotifyMessage(
                      It.IsAny<string>(),
                      It.Is<NotifyMessageRequest>(req =>
                        req.Message.Contains(
                            "OK: Branch [main/AST-001] was successfully merged"))),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == true)),
                    Times.Exactly(2));

                apiMock.RestApi.Verify(
                    mock => mock.MergeTo(
                      It.IsAny<string>(),
                      It.Is<MergeToRequest>(req => req.CreateShelve == false)),
                    Times.Exactly(2));

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMainShelveId))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch)),
                        It.Is<LaunchPlanRequest>(req => req.ObjectSpec.StartsWith("sh:" + mergeToMasterShelveId))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId1)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.
                    Verify(mock => mock.GetPlanStatus(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(buildId => buildId.Equals(ciBuildId2)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanBranch))),
                    Times.Once());

                apiMock.CiApi.Verify(
                    mock => mock.LaunchPlan(
                        It.Is<string>(ciName => ciName.Equals(config.CI.Plug)),
                        It.Is<string>(planName => planName.Equals(config.CI.PlanAfterCheckin)),
                        It.IsAny<LaunchPlanRequest>()),
                    Times.Never());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMainShelveId))),
                    Times.Once());

                apiMock.RestApi.Verify(
                    mock => mock.DeleteShelve(
                        It.Is<string>(repo => repo.Equals(config.Repository)),
                        It.Is<int>(id => id.Equals(mergeToMasterShelveId))),
                    Times.Once());

                Assert.AreEqual(ProcessBranch.Result.Failed, result);
            }
            finally
            {
                if (File.Exists(configTmpFile))
                    File.Delete(configTmpFile);
            }
        }
    }

    class RestApiMock
    {
        internal Mock<IRestApi> RestApi;
        internal Mock<IUsersApi> UsersApi;
        internal Mock<IMergeReportsApi> MergeReportsApi;
        internal Mock<IIssuesApi> IssuesApi;
        internal Mock<INotifyApi> NotifyApi;
        internal Mock<ICIApi> CiApi;
        internal Mock<ILabelApi> LabelsApi;
        internal Mock<IAttributeApi> AttrApi;
        internal Mock<ICodeReviewApi> CodeReviewApi;

        internal RestApiMock()
        {
            UsersApi = BuildUsersApi();
            MergeReportsApi = BuildMergeReportsApi();
            IssuesApi = BuildIssuesApi();
            NotifyApi = BuildNotifyApi();
            CiApi = BuildCIApi();
            LabelsApi = BuildLabelApi();
            AttrApi = BuildAttributeApi();
            CodeReviewApi = BuildCodeReviewApi();

            RestApi = new Mock<IRestApi>(MockBehavior.Loose);

            RestApi.Setup(mock => mock.Users).Returns(UsersApi.Object);
            RestApi.Setup(mock => mock.MergeReports).Returns(MergeReportsApi.Object);
            RestApi.Setup(mock => mock.Issues).Returns(IssuesApi.Object);
            RestApi.Setup(mock => mock.Notify).Returns(NotifyApi.Object);
            RestApi.Setup(mock => mock.CI).Returns(CiApi.Object);
            RestApi.Setup(mock => mock.Labels).Returns(LabelsApi.Object);
            RestApi.Setup(mock => mock.Attributes).Returns(AttrApi.Object);
            RestApi.Setup(mock => mock.CodeReviews).Returns(CodeReviewApi.Object);
        }

        static Mock<IUsersApi> BuildUsersApi()
        {
            Mock<IUsersApi> result = new Mock<IUsersApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<IMergeReportsApi> BuildMergeReportsApi()
        {
            Mock<IMergeReportsApi> result = new Mock<IMergeReportsApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<IIssuesApi> BuildIssuesApi()
        {
            Mock<IIssuesApi> result = new Mock<IIssuesApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<INotifyApi> BuildNotifyApi()
        {
            Mock<INotifyApi> result = new Mock<INotifyApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<ICIApi> BuildCIApi()
        {
            Mock<ICIApi> result = new Mock<ICIApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<ILabelApi> BuildLabelApi()
        {
            Mock<ILabelApi> result = new Mock<ILabelApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<IAttributeApi> BuildAttributeApi()
        {
            Mock<IAttributeApi> result = new Mock<IAttributeApi>(MockBehavior.Loose);
            return result;
        }

        static Mock<ICodeReviewApi> BuildCodeReviewApi()
        {
            Mock<ICodeReviewApi> result = new Mock<ICodeReviewApi>(MockBehavior.Loose);
            return result;
        }
    }
}
