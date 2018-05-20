#r "paket: 
nuget FSharp.Core prerelease
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.DotNet.Cli prerelease
"

#load "./.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO.Globbing.Operators

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "

  DotNet.exec id "clean" ""
  |> ignore
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the app --- "

  DotNet.build id ""
)

Target.create "SourceLink" (fun _ ->
  Trace.log " --- Building the app --- "

  DotNet.build id ""
)

Target.create "Test" (fun _ ->
  Trace.log " --- Testing the app --- "

  let setDotNetOptions (projectDirectory:string) : (DotNet.Options-> DotNet.Options)=
    fun (dotNetOptions:DotNet.Options) -> { dotNetOptions with WorkingDirectory = projectDirectory}

  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.map Path.GetDirectoryName
  |> Array.Parallel.map (fun projectDirectory -> DotNet.exec (setDotNetOptions projectDirectory) "xunit" "")
  |> ignore
)

Target.create "Pack" (fun _ ->
  Trace.log " --- Publishing app --- "
  DotNet.pack id ""
)

Target.create "Push" (fun _ ->
  Trace.log " --- Deploying app --- "
  
)


// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "SourceLink"
  ==> "Test"
  ==> "Pack"
  ==> "Push"


// *** Start Build ***
Target.runOrDefault "Push"