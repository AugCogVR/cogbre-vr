# cogbre-vr

"cogbre" = Cognitive Binary Reverse Engineering

NOTE: This is the VR component for "cogbre." The nexus (server) component is at https://github.com/AugCogVR/cogbre-nexus

This application supports research of the [Auburn CSSE Program Understanding Lab](https://program-understanding.github.io/).

This is a Unity-based front-end to provide an immersive and egocentric experience for those who perform reverse engineering on binary programs. 

Click to watch demo on YouTube:
[![Video](https://img.youtube.com/vi/1WJEy9Earzw/maxresdefault.jpg)](https://www.youtube.com/watch?v=1WJEy9Earzw)

Built and tested with the Unity 2021.3.4f1 LTS editor.

# Getting Started

Here is a brief list of everything (that we can remember) that you need:
- [cogbre-nexus](https://github.com/AugCogVR/cogbre-nexus) (latest in repo)
  - Python 3 and various packages (currently v3.9.16)
  - [Oxide](https://github.com/Program-Understanding/oxide) (latest in repo)
    - [Ghidra](https://ghidra-sre.org/) (currently v10.3.2)
    - and potentially other tools that Oxide leverages
  - These tools have been most extensively tested in Linux (including Windows 11 WSL2 Ubuntu) and MacOS. 
  - They *should* work in Windows natively. 
- [cogbre-vr](https://github.com/AugCogVR/cogbre-vr) (latest in repo)
  - Unity (currently v2021.3.4f1)
    - MRTK2 (currently v2.8.3.0)
    - To support VR gear: SteamVR (currently v2.2.3)
    - You don't need VR gear for dev and basic testing
  - These tools have been most extensively tested in Windows (10/11) and MacOS (simulator mode only). 
  - They *should* work in Linux natively. 
- Dev tools: git, VSCode, etc. 


## Setting up Unity and this app

Download and install Unity Hub and editor version 2021.3.4f1. This project doesn't use modules other than those installed by default. 

NOTE: As of this writing, 2021.3.21f1 is the latest LTS, and this app *probably* works with it. However, if you use it, you *might* introduce problems for others working on this project using 2021.3.4f1. Yeah, we should all just update, but in this case it's "leave well enough alone."

Add the project to Unity Hub and open it. Open "SampleScene" from Assets/Scenes. 

Any required packages that aren't included in the baseline Unity installation should be in the project and repo so no additional manual package installations should be required.


## VR Device Setup Notes

### HTC Vive

This app (so far) is designed for the HTC Vive Pro. This is what I had to do to make things work. YMMV.

- To prevent SteamVR Home from starting instead of the Unity VR app: SteamVR -> Settings -> (enable Advanced Settings) -> SteamVR Home OFF
- To set up SteamVR to work with OpenXR: SteamVR -> Settings -> Developer -> Current OpenXR Runtime: SteamVR
- To add support for HTC Vive controllers, in Unity Project Settings, ensure in XR Plug-in Management -> OpenXR to add Interaction Profile for HTC Vive Controller 
- If SteamVR insists on a room setup too often, a solution seems to be to manually run Vive Console to start SteamVR before doing VR stuff. See https://steamcommunity.com/app/250820/discussions/0/3040480988282735041/ 



# Running it

Set up and start the Nexus: https://github.com/AugCogVR/cogbre-nexus

NOTE: For now, the VR component is hardcoded to look for the server on 127.0.0.1

Load the VR project into Unity, open the main scene ("Abstraction 2"), and run it.


# Architecture

![basic architecture diagram](basic_architecture.jpg)

The fundamental concepts behind this architecture are:
- The "heavy lifting" happens in the Server (Nexus) component 
- The VR component periodically polls the Server for updates
- The VR experience runs with minimal interruption to ensure a high frame rate
- The Server can take advantage of the myriad Python packages to support various anticipated operations


# Design

The bulk of the custom-developed code and components is in Assets/PUL. Some custom items are in Assets/Resources. 

## Main Loop / Game Manager

We use the "Game Manager" pattern, inspired by https://bronsonzgeb.com/index.php/2021/04/24/unity-architecture-pattern-the-main-loop/

See Assets/PUL/Managers/GameManager for code

The basic idea is: new objects in the world are registered with the Game Manager. The Unity engine calls `GameManager.Update`, and that method calls the Start and Update methods of the other objects in the world (rather than the Unity engine). In those objects, the methods are named `OnStart` and `OnUpdate`. 


## Data Exchange

We connect to the Nexus using its RESTful API, inspired by https://www.red-gate.com/simple-talk/development/dotnet-development/calling-restful-apis-unity3d/

The GameManager creates a NexusClient (see Assets/PUL/DataClients/NexusClient). 

The NexusClient periodically polls the server for updates using a separate thread to avoid delaying the Unity game loop. Inspired by https://www.red-gate.com/simple-talk/development/dotnet-development/calling-restful-apis-unity3d/ 


## Visualization Components

The various components, behaviors, etc. that can/will be used to create the visualization are in Assets/PUL/VizComponents. Right now, it's a set of very basic components to display code excerpts, call graphs, and control flow graphs. 



