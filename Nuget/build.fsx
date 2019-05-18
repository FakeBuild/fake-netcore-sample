#r "paket:

source https://api.nuget.org/v3/index.json

nuget Fake.BuildServer.TeamFoundation
nuget Fake.Core.Environment
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git
nuget Fake.Tools.GitVersion
nuget GitVersion.CommandLine storage:packages
nuget Microsoft.Extensions.Configuration.UserSecrets //"

#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif

open Fake.BuildServer
open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.Tools
open Microsoft.Extensions.Configuration
open System.IO

(****************************************************************************************
                                  Configuration
****************************************************************************************)

let isTeamFoundationBuild = Environment.environVarOrNone "TF_BUILD"

if isTeamFoundationBuild.IsSome then
  Trace.log "Build server detected : TeamFoundation"
  BuildServer.install [ TeamFoundation.Installer ]
else
  Trace.log "Build server detected : None"

let buildConfiguration = DotNet.BuildConfiguration.Release
let packageOutputFolder = sprintf "%s\\MyRepoNameNugets" Fake.SystemHelper.Environment.Environment.CurrentDirectory

(****************************************************************************************
                            Git / GitVersion specific code
****************************************************************************************)
let branchName = Git.Information.getBranchName "."
let isPreview  = branchName <> "master"

let getPackageVersion() =
  Trace.log " --- Computing version --- "
  let gitVersionResult = GitVersion.generateProperties (fun p -> { p with ToolPath = ".fake/build.fsx/packages/GitVersion.CommandLine/tools/GitVersion.exe" })
  let branchName = Git.Information.getBranchName "."
  let version =
    match isPreview with
    | true -> sprintf "%d.%d.0-%s-%d" gitVersionResult.Major gitVersionResult.Minor branchName gitVersionResult.CommitsSinceVersionSource
    | false  -> sprintf "%d.%d.%d"  gitVersionResult.Major gitVersionResult.Minor gitVersionResult.CommitsSinceVersionSource
  Trace.logf "Version is : %s " version
  Trace.setBuildNumber version
  version
let packageVersion = lazy ( getPackageVersion() )

(****************************************************************************************
           Nuget Repository Feed and ApiKey (From either EnvVar or User Secrets)
****************************************************************************************)

let buildUserSecretsId = "MyRepoName.Build"
let getUserSecret (userSecretsId:string) secretName =
  (******************************************************************************************************
    dotnet user-secrets --id MyRepoName.Build list --json

    dotnet user-secrets --id MyRepoName.Build set MYGET_PRERELEASE_FEED       X
    dotnet user-secrets --id MyRepoName.Build set MYGET_PRERELEASE_APIKEY     X
    dotnet user-secrets --id MyRepoName.Build set NUGET_RELEASE_FEED          X
    dotnet user-secrets --id MyRepoName.Build set NUGET_RELEASE_APIKEY        X

  *******************************************************************************************************)

  let configuration = ConfigurationBuilder()
                              .AddUserSecrets(userSecretsId)
                              .Build()
  configuration.GetSection(secretName).Value

let getBuildUserSecret = getUserSecret buildUserSecretsId

let getEnvVarOrUserSecrets =
  match BuildServer.isLocalBuild with
  | true  -> getBuildUserSecret
  | false -> Environment.environVarOrFail

type Repository =
  {
    feed: string
    apiKey: string
  }

[<Literal>]
let Nuget = "NUGET_RELEASE"

[<Literal>]
let MyGet = "MYGET_PRERELEASE"

let getFeedVarName configuration repositoryName =
  sprintf "%s_%s_FEED" repositoryName configuration

let getApiKeyVarName configuration repositoryName =
  sprintf "%s_%s_APIKEY" repositoryName configuration

let getRepository configuration repositoryName =
  {
    feed = getEnvVarOrUserSecrets (getFeedVarName configuration repositoryName);
    apiKey = getEnvVarOrUserSecrets (getApiKeyVarName configuration repositoryName);
  }

let getRepositories =
  match isPreview with
  | true  -> [getRepository "PRERELEASE" MyGet]
  | false -> [getRepository "RELEASE" Nuget]

(****************************************************************************************
                                      DotNetCli
****************************************************************************************)

let setDotNetBuildOptions : (DotNet.BuildOptions -> DotNet.BuildOptions) =
  fun (dotNetBuildOptions:DotNet.BuildOptions) ->
    { dotNetBuildOptions with
        Configuration = buildConfiguration
        Common        = { dotNetBuildOptions.Common with CustomParams = Some "--no-restore" }
    }

let setDotNetTestOptions (projFilePath:string) : (DotNet.TestOptions-> DotNet.TestOptions) =
  let projectDirectory = Path.GetDirectoryName projFilePath
  fun (dotNetTestOptions:DotNet.TestOptions) ->
    { dotNetTestOptions with
        Common        = { dotNetTestOptions.Common with WorkingDirectory = projectDirectory }
        NoBuild       = true
        Configuration = buildConfiguration
    }

let setDotNetPackOptions (nupkgTargetFolder:string) (packageVersion:string) : (DotNet.PackOptions-> DotNet.PackOptions) =
  fun (dotNetPackOptions:DotNet.PackOptions) ->
    { dotNetPackOptions with
        OutputPath    = Some nupkgTargetFolder
        Configuration = buildConfiguration
        NoBuild       = true
        Common        = { dotNetPackOptions.Common with CustomParams = Some (sprintf "/p:PackageVersion=\"%s\"" packageVersion) }
    }

//run dotnet CLI in parallal per folder matching the globbing pattern (used for UnitTests / IntegrationTest / push)
let executeInParallal globbingPattern callback =
  !!(globbingPattern)
  |> Seq.toArray
  |> Array.Parallel.iter callback

//dotnet test need to be ran in the proj folder
let dotNetTestProject projFilePath =
    DotNet.test (setDotNetTestOptions projFilePath) ""

//run dotnet CLI in parallal per folder matching the globbing pattern (used for UnitTests / IntegrationTest / push)
let dotNetTestFromGlobbingPattern globbingPattern =
  executeInParallal globbingPattern dotNetTestProject

let dotNetPackToFolder nupkgTargetFolder =
  let packOptions = setDotNetPackOptions nupkgTargetFolder (packageVersion.Force())
  DotNet.pack packOptions ""

(****************************************************************************************
                                  Target implementations
****************************************************************************************)
let targetGitVersion _ =
  packageVersion.Force()
  |> ignore

let targetClean _ =
  Trace.log " --- Cleaning stuff --- "
  if Directory.Exists(packageOutputFolder) then
    Directory.Delete(packageOutputFolder, true)

  DotNet.exec id "clean" "--configuration Release"
  |> ignore

let targetRestore _ =
  Trace.log " --- Restore at solution level --- "
  DotNet.exec id "restore" ""
  |> ignore

let targetBuild _ =
  Trace.log " --- Building the solution --- "
  DotNet.build setDotNetBuildOptions ""

let targetUnitTests targetParams =
  if List.exists ((=) "--skip-unit-tests") targetParams.Context.Arguments then
    Trace.log " --- Skipping Unit Tests --- "
  else
    Trace.log " --- Running Unit Tests projects in parallal --- "
    dotNetTestFromGlobbingPattern "test/**/*.Tests.??proj"

let targetIntegrationTests targetParams =
  if List.exists ((=) "--skip-integration-tests") targetParams.Context.Arguments then
    Trace.log " --- Skipping Integration Tests --- "
  else
    Trace.log " --- Running Integration Tests projects in parallal --- "
    dotNetTestFromGlobbingPattern "test/**/*.IntegrationTests.??proj"

let targetPack _ =
  Trace.log " --- Packaging nugets app --- "
  dotNetPackToFolder packageOutputFolder

let targetPush _ =
  Trace.log " --- Pushing nuget --- "

  let getNugetPushArgs repository folder =
    sprintf "nuget push %s\\*.nupkg -s %s -k %s" folder repository.feed repository.apiKey

  let pushNugets repositories folder =
   repositories
   |> List.iter (fun repository ->
        let nugetPushArgs = getNugetPushArgs repository folder
        Trace.logfn "Pushing nuget with : 'dotnet %s'" (nugetPushArgs.Replace(repository.apiKey, "***"))
        let processResult = DotNet.exec id nugetPushArgs ""
        if processResult.OK then
          Trace.logfn "Nuget pushed to '%s'" repository.feed
        else
          Trace.traceErrorfn "(trace) Could not push nuget, error messages :\n%A" processResult
          failwithf "Could not push nuget, error messages :\n%A" processResult
    )

  pushNugets getRepositories packageOutputFolder


(****************************************************************************************
                                  Define Targets
****************************************************************************************)
Target.create "GitVersion" targetGitVersion
Target.create "Clean" targetClean
Target.create "Restore" targetRestore
Target.create "Build" targetBuild
Target.create "UnitTests" targetUnitTests
Target.create "IntegrationTests" targetIntegrationTests
Target.create "FullTests" ignore
Target.create "Pack" targetPack
Target.create "Push" targetPush
Target.createFinal "Done" (fun _ -> Trace.log " --- Fake script is done --- ")

(****************************************************************************************
                           Define Target ordering dependencies
****************************************************************************************)
open Fake.Core.TargetOperators

let shouldPush = not BuildServer.isLocalBuild

"Clean"
==> "Restore"
==> "Build"
==> "Pack"
=?> ("Push", shouldPush)
==> "Done"

"GitVersion"
==> "Pack"

"Build"
==> "Pack"

"Build"
==> "UnitTests"
==> "FullTests"

"Build"
==> "IntegrationTests"
==> "FullTests"

"FullTests"
=?> ("Push", shouldPush)

"FullTests"
==> "Done"


(****************************************************************************************
                                     Start Build
****************************************************************************************)
Target.runOrDefaultWithArguments "Done"
