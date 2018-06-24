#r "paket: 
nuget FSharp.Core
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
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
  Trace.log " --- Building the app --- "

  DotNet.build id ""
)

Target.create "Test" (fun _ ->
  Trace.log " --- Testing projects in parallal --- "

  let setDotNetOptions (projectDirectory:string) : (DotNet.TestOptions-> DotNet.TestOptions) =
    fun (dotNetTestOptions:DotNet.TestOptions) -> 
      { dotNetTestOptions with
          Common        = { dotNetTestOptions.Common with WorkingDirectory = projectDirectory}
          Configuration = DotNet.BuildConfiguration.Release
      }

  //Looks overkill for only one csproj but just add 2 or 3 csproj and this will scale a lot better
  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.iter (
    fun fullCsProjName -> 
      let projectDirectory = Path.GetDirectoryName(fullCsProjName)
      DotNet.test (setDotNetOptions projectDirectory) ""
    )
)

Target.create "Publish" (fun _ ->
  Trace.log " --- Publishing app --- "

  //Publishing a specific csproj
  let setPublishParams (defaultPublishParams :DotNet.PublishOptions) = 
    { defaultPublishParams with
        Common = { defaultPublishParams.Common with WorkingDirectory = "src\\Poc.Cli" }
    }

  DotNet.publish setPublishParams "Poc.Cli.csproj"
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
  ==> "Publish"
  ==> "Done"


// *** Start Build ***
Target.runOrDefault "Done"