# Scripts for showing OpenSesame stimuli inside Unity

A set of scripts to connect the OpenSesame experiment builder to the Unity game engine. The main goal of this is to allow OpenSesame experiments to project into a virtual-reality headset.

Currently, the stimulus display from OpenSesame is project as the skybox (background) of the Unity scene.

## Unity instructions

### Creating a custom skybox

- Within Assets, right click and select Create -> Material
- Call the new material CustomSkyboxMaterial
- Under Shader, select Skybox/Panoramic
- Under Image Type, select 180 Degrees
- Now open Menu -> Window -> Rendering -> Lighting
- Open the Environment tab of the Lighting window
- Under Skybox Material, select CustomSkyboxMaterial

## Insert the listener (server) script

- Within Assets, right click and select Create -> C# Script
- Call the new script OpenSesameListener
- Edit the script (OpenSesameListener.cs) and replace the contents with those from OpenSesameListener.cs from this repository
- Right click in the scene and select 

Create a new C# script in the 
