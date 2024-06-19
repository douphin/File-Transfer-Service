# File-Transfer-Service
A Windows Service designed to automatically transfer files from multiple sources to a chosen destination. The service is set up to systematically copy files that are routinely created.

## Service Build Instructions
Open FileTransfer.sln using Visual Studio. In the solution Explorer, select RevisedFileTransferService, then at the top of the window select Build->Publish Selection. Create a new profile, target: Folder, input your desired folder path. For the settings,
+ Configuration: Release | Any CPU;
+ Target framework: net6.0;
+ Deployment Mode: Self-contained;
+ Target Runtime: win-x64;
+ Target Location: \your\location;
+ File Publish Options:
  - Produce Single File: Yes;
  - Enable ReadyToRun Compilation: Yes;
  - Trim unused code: optional;

Once these settings are dialed in, click publish and wait for the service to be compiled. At this point, if the published .exe file isn't where you want it to run, then move it to the correct location. Next run the following command in powershell:
sc.exe create "ServiceName" binpath= "C:\Path\To\App.WindowsService.exe". The service can now be started and ran. Initial errors may be published to the windows application event viewer.

More details can be found [here](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service#publish-the-app)

### Common errors:
  -Not creating JSONs for the service to read/write from. A template for what the JSONs should look like can be found in the UnitTestsFileTransferService project.
  -Not changing file paths for JSONs and logging. Said file paths can be found at the top of WindowsBackgroundService.cs

## Console App Build Instructions
Open FileTransfer.sln using Visual Studio. In the solution Explorer, select 32Bit_FileTransferSQLQueries, then at the top of the window select Build->Publish Selection. Create a new profile, target: Folder, input your desired folder path. For the settings,
+ Configuration: Release | Any CPU;
+ Target framework: net8.0;
+ Deployment Mode: Self-contained;
+ Target Runtime: win-x86;
+ Target Location: \your\location;
+ File Publish Options:
  - Produce Single File: Yes;
  - Enable ReadyToRun Compilation: Yes;
  - Trim unused code: optional;

Once these settings are dialed in, click publish and wait for the service to be compiled. At this point, if the published .exe file isn't where you want it to run, then move it to the correct location.

More details can be found [here](https://learn.microsoft.com/en-us/dotnet/core/tutorials/publishing-with-visual-studio?pivots=dotnet-8-0)

### Common Errors
  -Not setting target runtime to 32 bit, because this is built to use a 32bit database driver, the app will only work if compiled as 32 bit.
  -Not setting file paths to correct locations. Because this will write outputs to a txt file that the service will read in, correct file paths need to be set for both the service and the app. Check ~Service\SQLfunctions.cs and ~App\Program.cs

## Unit Test Build Instructions
Open FileTransfer.sln using Visual Studio. In the solution Explorer, right click the UnitTestsFileTransferService project, and from the drop down click "Show in Test Explorer". You may need to click this optino a couple times for the Test Explorer to show up. At this point, use the green arrow at the top of the text explorer to run tests.
