@ECHO OFF

SET BUILD_PACKAGES=BuildPackages
SET FAKE_CLI="%BUILD_PACKAGES%/fake.exe"

REM IF EXIST %FAKE_CLI% (
REM   ECHO "Deleting '%BUILD_PACKAGES%' folder"
REM   RMDIR /Q /S "%BUILD_PACKAGES%"
REM )

IF NOT EXIST %FAKE_CLI% (
  CALL updateFakeCli.cmd
)

%FAKE_CLI% run build.fsx --target "Done"
