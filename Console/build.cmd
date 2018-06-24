@ECHO OFF

SET BUILD_PACKAGES=BuildPackages
SET FAKE_CLI="%BUILD_PACKAGES%/fake.exe"

IF NOT EXIST %FAKE_CLI% (
  dotnet tool install fake-cli --tool-path ./%BUILD_PACKAGES%
)

REM comments following lines once you are done with your script, the idea is to be sure paket install regenerate the lock file if we add new nuget in the fsx
REM IF EXIST ".fake"          (RMDIR /Q /S ".fake"         )
REM IF EXIST "build.fsx.lock" (DEL         "build.fsx.lock")

%FAKE_CLI% run build.fsx --target "Done"