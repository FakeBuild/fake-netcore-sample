@ECHO OFF

SET BUILD_PACKAGES=.fake
SET FAKE_CLI="%BUILD_PACKAGES%/fake.exe"

IF EXIST %FAKE_CLI% (
  ECHO "Deleting '%BUILD_PACKAGES%' folder"
  RMDIR /Q /S "%BUILD_PACKAGES%"
)

IF NOT EXIST %FAKE_CLI% (
  CALL updateFakeCli.cmd
)

%FAKE_CLI% run build.fsx --target "Done"
