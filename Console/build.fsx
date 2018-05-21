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
  Trace.log " --- Testing the app --- "

  let setDotNetOptions (projectDirectory:string) : (DotNet.Options-> DotNet.Options)=
    fun (dotNetOptions:DotNet.Options) -> { dotNetOptions with WorkingDirectory = projectDirectory}

  //Looks overkill for only one csproj but just add 2 or 3 csproj and this will scale a lot better
  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.map Path.GetDirectoryName
  |> Array.Parallel.map (fun projectDirectory -> DotNet.exec (setDotNetOptions projectDirectory) "xunit" "")
  |> ignore
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