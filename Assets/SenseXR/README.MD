# SenseXR Unity project

This project allows you to use the following features of SenseXR in Unity:

- Perform SenseXR Data Capture
  - Collect SenseXR Simulation Events (Session Events and Interaction Events)
  - Collect SenseXR Time Series Data
  - Collect Emotibit Sensor Data as Time Series Data

## Structure

The package contains the following noteworthy folders and files:

- _SenseXR_ folder - Containing both Core and Demo assets
  - The _Scenes_ folder - Contains a demo scene that shows how data can be captured through this library (including Emotibit sensor data)
  - The _Scripts_ folder - Contains both Demo and essential (Core and Utils) scripts.
- Emotibit folder - Contains the Emotibit library and SenseXR integration scripts
  - The Core folder with all the necessary scripts to integrate Emotibit with SenseXR
  - The OSC folder with the OSC library used to communicate with Emotibit. Can be used as an alternative to the Emotibit Wifi capture library
  - The Demo folder with prefabs that show how to capture Emotibit data through SenseXR

## How to Use (Demo)

You can begin by exploring the proposed demo flow:

- Add SenseXR/Scenes/Demo/GameplayDemoScene to your "Scenes in Build" 
- Follow the onscreen instructions. If the emotibit is connected, sensor data should be shown in the screen. Even if no emotibit is connected, SenseXR data from the sample "Cube" object and interactions from the user are collected.
- You should have collected JSON and CSV files in the designated SenseXR folder
  - If building for Android, the files should be saved in the data folder of the application (usually "Android/data/com.XXX.YYY/files/SenseXR/[session_id])
  - If running on the Editor / Windows, the files should be saved in the SenseXR folder in "MyDocuments"

## How to Integrate

The package can be imported into a Unity project. Please check the CubeDemo class for an example on how to:

- Collect Simulation events (such as starting / ending a Simulation session)
- Collect Interaction events (a key press example event is shown)
- Collect Time series data (an example is shown where the velocity of the cube is collected over time)
- How to wrapup the collection of data by publishin the results

If Emotibit data capture is also required, please check the EmotibitOSCCollector and EmotibitWifiCollector classes.

**In your simulation, you should:**

- Initialize the _StatistcsManager_ instance
- (optional) Initialize a unique identifier for the user (userID) whenever needed. A GUID is suggested.
- Register Simulation Events for the start and ending of a simulation
- (optional) Register Interaction events that represent events of interest to the simulation in a given moment in time (e.g.: a key press, the user completing or attempting a certain task, etc.)
- (optional) Register Time Series data that represent how certain values change over time (heart rate, gaze, direction, position, etc.) that are of relevance for the application scope

## Notes and Warnings

This is a trimmed down version of the HEAT Unity plugin by VRAI, aimed for simple data collection that is still HEAT compatible. It should run correctly on Windows, Android, iOS and Web Player, but changes may be necessary depending on the target platform, hardware and the application itself
The Emotibit library is included in the project, but may be removed or extracted if needed. It is included to simplify the import and integration process