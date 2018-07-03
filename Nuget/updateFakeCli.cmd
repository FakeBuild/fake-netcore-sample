@ECHO OFF

SET BUILD_PACKAGES=.fake
SET FAKE_CLI="%BUILD_PACKAGES%/fake.exe"

IF EXIST %FAKE_CLI% (
  ECHO "Deleting '%BUILD_PACKAGES%' folder"
  RMDIR /Q /S "%BUILD_PACKAGES%"
)

IF NOT EXIST %FAKE_CLI% (
  ECHO "Installing 'fake-cli' dotnet tool"
  dotnet tool install fake-cli --tool-path ./%BUILD_PACKAGES%
)

IF EXIST ".fake" (
  ECHO "Deleting folder '.fake'"
  RMDIR /Q /S ".fake"
)

IF EXIST "build.fsx.lock" (
  ECHO "Deleting file 'build.fsx.lock'"
  DEL "build.fsx.lock"
)
