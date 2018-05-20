#r "paket: 
nuget FSharp.Core prerelease
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.Testing.Xunit2 prerelease
"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "

  let setDotNetOptions defaultDtNetOptions : DotNet.Options =
    defaultDtNetOptions

  DotNet.exec setDotNetOptions "clean" ""
  |> ignore
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the app --- "

  let setBuildParams defaultBuildParams = 
    {defaultBuildParams with }

  DotNet.build id ""
)

Target.create "Test" (fun _ ->
  Trace.log " --- Testing the app --- "

  !!("test/**/*.Tests.dll")
  |> XUnit2.run id
)

Target.create "Publish" (fun _ ->
  Trace.log " --- Publishing app --- "
  DotNet.publish id ""
)

Target.create "Deploy" (fun _ ->
  Trace.log " --- Deploying app --- "
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Test"
  ==> "Publish"
  ==> "Deploy"


// *** Start Build ***
Target.runOrDefault "Deploy"