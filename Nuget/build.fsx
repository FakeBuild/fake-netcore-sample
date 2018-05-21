#r "paket: 
nuget System.Reactive.Linq
nuget System.Reactive.Core
nuget FSharp.Core prerelease
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.DotNet.Cli prerelease
"

#load "./.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.DotNet

//*********************************************************/
// *** Target implementations
//*********************************************************/

let targetClean _ =
  Trace.log " --- Cleaning stuff --- "
  DotNet.exec id "clean" ""
  |> ignore

let targetBuild _ =
  Trace.log " --- Building the solution --- "
  DotNet.build id ""

let targetTest _ =
  Trace.log " --- Testing projects in parallal --- "

  let setDotNetOptions (projectDirectory:string) : (DotNet.Options-> DotNet.Options)=
    fun (dotNetOptions:DotNet.Options) -> { dotNetOptions with WorkingDirectory = projectDirectory}

  //Looks overkill for only one csproj but just add 2 or 3 csproj and this will scale a lot better
  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.map Path.GetDirectoryName
  |> Array.Parallel.map (fun projectDirectory -> DotNet.exec (setDotNetOptions projectDirectory) "xunit" "")
  |> ignore 

let targetPack _ =
  Trace.log " --- Packaging nugets app --- "
  DotNet.pack id "" //--output FOLDERHERE"

let targetPush _ =
  Trace.log " --- Deploying app --- "

  let nugetPushArgs = 
    let source = Environment.environVarOrFail "NUGET_FEED_TO_PUSH"
    let apiKey = Environment.environVarOrFail "SOURCE_NUGET_API_KEY"

    sprintf "nuget push -s %s -k %s" source apiKey

  DotNet.exec id nugetPushArgs
  |> ignore

//*********************************************************/
// *** Define Targets ***
//*********************************************************/
Target.create "Clean" targetClean
Target.create "Build" targetBuild
Target.create "Test" targetTest
Target.create "Pack" targetPack
Target.create "Push" targetPush
Target.createFinal "Done" (fun _ -> Trace.log " --- Fake script is done --- ")

//*********************************************************/
//                   TARGETS ORDERING
//*********************************************************/
open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Test"
  ==> "SourceLink"
  ==> "Pack"
  =?> ("Push", BuildServer.isLocalBuild)
  ==> "Done"

// *** Start Build ***
Target.runOrDefault "Done"