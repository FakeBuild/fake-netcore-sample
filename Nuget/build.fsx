#r "paket: 
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


// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "

  DotNet.exec id "clean" ""
  |> ignore
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the solution --- "

  DotNet.build id ""
)

Target.create "Test" (fun _ ->
  Trace.log " --- Testing projects in parallal --- "

  let setDotNetOptions (projectDirectory:string) : (DotNet.Options-> DotNet.Options)=
    fun (dotNetOptions:DotNet.Options) -> { dotNetOptions with WorkingDirectory = projectDirectory}

  //Looks overkill for only one csproj but just add 2 or 3 csproj and this will scale a lot better
  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.map Path.GetDirectoryName
  |> Array.Parallel.map (fun projectDirectory -> DotNet.exec (setDotNetOptions projectDirectory) "xunit" "")
  |> ignore
)

Target.create "SourceLink" (fun _ ->
  Trace.log " --- Running SourceLink --- "

  // DotNet.build id ""
)

Target.create "Pack" (fun _ ->
  Trace.log " --- Packaging nugets app --- "

  DotNet.pack id "" //--output FOLDERHERE"
)

Target.create "Push" (fun _ ->
  Trace.log " --- Deploying app --- "
  
  // DotNet.exec id "nuget" "push [some other args here]"
  // |> ignore
)

Target.createFinal "Done" (fun _ ->
  Trace.log " --- Fake script is done --- "
)

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
  ==> "Push"
  ==> "Done"

// *** Start Build ***
Target.runOrDefault "Push"