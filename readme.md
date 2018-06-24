FakeBuild samples for DotNet
=

This repository contains multiple dotnet samples using Fake CLI as a build system  
`fake-cli` is not used as a `global` install on purpose. It uses a `cmd` to install it in a `BuildPackages` folder so that the build would fetch everything it depends on  
The idea is that a build system should not required pre-installed artifacts to run

A wildcard resolution is used/needed for `--version` until Fake 5 release, once release the version could be deleted
```cmd
SET BUILD_PACKAGES=BuildPackages
SET FAKE_CLI="%BUILD_PACKAGES%/fake.exe"    

IF NOT EXIST %FAKE_CLI% (
  dotnet tool install fake-cli --tool-path ./%BUILD_PACKAGES%
)
```

# Work in progress
## Console
This folder contains a really simple scenario :
```cmd
> dotnet new console
> dotnet new xunit
> dotnet new sln
```

* `tasks.json` allow you to run `FakeBuild`
* `launch.json` allow you to debug the `fsx` script
* `clean` and `build` Target are using the `sln` based dotnet behavior
* `test` look for every single `*.Tests.csproj` in the `test` folder and run `dotnet xunit` in every single of their parent folder. This step runs them all in parallel, to understand why just add few `xunit` project to the test folder and replace
  ```fsharp
  !!("test/**/*.Tests.csproj")
  |> Seq.toArray
  |> Array.Parallel.map ...
  |> Array.Parallel.map (fun ...
  ```
  with
  ```fsharp
  !!("test/**/*.Tests.csproj")
  |> Seq.map ...
  |> Seq.map (fun ...
  ```

# To be done / Nothing works yet, just trying to create basic use cases
_*The following are just guidelines*_

## Nuget
Intended to show the use of a solution containing multiple nugets to produce :
* `dotnet pack`
* `sourcelink`
* `dotnet nuget push` (Not sure were to push for now and where to get credential)

## Mvc
* `dotnet build`
* `dotnet test`
* `dotnet publish`
* Deploy to (local ?) iis


## Front
### Simple app
Using Fake with Npm stuff (to prepare a more complex scenario)
* Npm install
* Npm build
* Npm test
* maybe some vscode/webpack integration for debug

### Publishing an npm package ?

## Complex : mixing everything together
* Web Api (hosted in IIS / Azure)
* Front App
* Console to run a backend (the idea is to get multiple package out of it)
* Test
* Sonar
* Deploy front to a CDN ?
* Deploy back to Azure ?