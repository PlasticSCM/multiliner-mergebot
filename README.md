# Multiliner mergebot

Each task branch is built, tested, and merged to the specified branches in an attribute of the processed task branch


# Build
The executable is built from .NET Framework code using the provided `src/multiliner-mergebot.sln`
solution file. You can use Visual Studio or MSBuild to compile it.

**Note:** We'll use `${DEVOPS_DIR}` as alias for `%PROGRAMFILES%\PlasticSCM5\server\devops`
in *Windows* or `/var/lib/plasticscm/devops` in *macOS* or *Linux*.

# Setup
If you just want to use the built-in multilinerbot you don't need to do any of this.
The multilinerbot is available as a built-in mergebot in the DevOps section of the WebAdmin.
Open it up and configure your own!

## Configuration files
You'll notice some configuration files under `/src/configuration`. Here's what they do:
* `multilinerbot.log.conf`: log4net configuration. The output log file is specified here. This file should be in the binaries output directory.
* `multilinerbot.definition.conf`: mergebot definition file. You'll need to place this file in the Plastic SCM DevOps directory to allow the system to discover your multilinerbot.
* `multilinerbot.config.template`: mergebot configuration template. It describes the expected format of the multilinerbot configuration. We recommend to keep it in the binaries output directory
* `multilinerbot.conf`: an example of a valid multilinerbot configuration. It's built according to the `multilinerbot.config.template` specification.

## Add to Plastic SCM Server DevOps
To allow Plastic SCM Server DevOps to discover your custom multilinerbot, just drop 
the `multilinerbot.definition.conf` file in `${DEVOPS_DIR}/config/mergebots/available$`.
Make sure the `command` and `template` keys contain the appropriate values for
your deployment! Your custom mergebot will be listed in the mergebot types page of
the WebAdmin under the "Custom" section.

# Behavior
The **Multiliner Bot** merges branches to multiple lines of development based on a per-branch configuration. 
It's connected to the Plastic SCM Server, waiting for branches
to be set to **resolved**. It also retrieves task information from **issue trackers**,
triggers builds in external **CI systems** and it's able to **notify** the team
about the progress and results of these operations.

Once the multiliner bot picks a branch, it fetches the value of its attribure (attribute name defined in the multiliner bot configuration).
This value should be a list of comma-separated merge destination branches (full branch name with no repspec part!) for this picked branch.

**Consider the following scenario:**
The multiliner bot is configured to track all `j-*` branches, and detects that **j-341** is set to `resolved`. 
The branch also has the attribute `mergebranch` set to **iter-13**, so the bot knows it has to merge **j-341** to **iter-13**. 


<p align="center">
  <img alt="Scenario 1 initial diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot01.png" />
</p>

The **multiliner** bot successfully merges **j-341** into **iter-13** and sets the status of **j-341** to `testing` so that everyone in the team knows what is happening. 

<p align="center">
  <img alt="Scenario 1 merge to shelveset diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot02.png" />
</p>

The merge result is stored on a shelve, that **won't be commited to the branch** **iter-13** until the tests pass.

Now multiliner uses the Continuous Integration plug to send the result of the merge to a CI system and test the merge.
If the build or the test fail, it will notify the team using a notifier plug. 

<p align="center">
  <img alt="DevOps diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot03.png" />
</p>

If the build is correct and tests pass, then multiliner confirms the merge in the branch. The branch **j-341** is now correctly merged.

The bot notifies the team and the author of the branch using the configured channels (email, Slack, etc.). 


<p align="center">
  <img alt="Scenario 1 merge to changeset confirmed diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot04.png" />
</p>

The **multiliner** bot can also handle multiple destinations. Consider the following example: 

<p align="center">
  <img alt="Scenario 2 initial diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot05.png" />
</p>

The **hotfix-943** is ready, and must be merged to **b3.0** and also to **b3.1**.

The **multiliner** bot will merge first **hotfix-943** to the two branches, creating two shelves.
If the merge fails, the task will be rejected. If the two merges succeed, then the two shelves will be sent to the CI for testing.
The merges will only only be confirmed if both pass the tests, otherwise the branch **hotfix-943** will be rejected: 

<p align="center">
  <img alt="Scenario 2 merge to shelveset diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot06.png" />
</p>

If merges are confirmed, it means both branches were tested correctly. 

<p align="center">
  <img alt="Scenario 2 merge to changeset confirmed diagram" src="https://raw.githubusercontent.com/PlasticSCM/multiliner-mergebot/master/doc/img/picture-devops-multilinerbot07.png" />
</p>

# Support
If you have any questions about this mergebot don't hesitate to contact us by
[email](support@codicesoftware.com) or in our [forum](http://www.plasticscm.net)!
