#r "paket: 
open Fake.DotNet.Testing.XUnit2
nuget FSharp.Core prerelease
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.Testing.Xunit2 prerelease
"

#load "./.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.DotNet
// open Fake.DotNet.Testing

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "

  DotNet.exec id "clean" ""
  |> ignore
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the app --- "

  // Using the test project to build both Project and unit tests
  let setBuildParams (defaultBuildParams :DotNet.BuildOptions) = 
    { defaultBuildParams with
        Common = { defaultBuildParams.Common with WorkingDirectory = "test\\Poc.Cli.Tests" }
    }

  DotNet.build setBuildParams "Poc.Cli.Tests.csproj"
)

// Target.create "Test" (fun _ ->
//   Trace.log " --- Testing the app --- "
//   let setXUnitParams (defaultXUnitParams :XUnit2.XUnit2Params) = 
//     { defaultXUnitParams with
//         Parallel = XUnit2.ParallelMode.All
//         WorkingDir = Some "test\\Foo.Tests"
//     }
//   XUnit2.run setXUnitParams [||]
// )

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

Target.create "Publish" (fun _ ->
  Trace.log " --- Publishing app --- "

  let setPublishParams (defaultPublishParams :DotNet.PublishOptions) = 
    { defaultPublishParams with
        Common = { defaultPublishParams.Common with WorkingDirectory = "src\\Poc.Cli" }
    }

  DotNet.publish setPublishParams "Poc.Cli.csproj"
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