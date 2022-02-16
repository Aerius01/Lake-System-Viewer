If you want to move around in the scene, you will need to add an FPS controller.  You can you use the one included in the standard assets.
In the Unity editor, go to Assets < Import Package < Characters, and import all the assets.
In the project window, go to Assets < Standard Assets < Characters < FirstPersonCharacter < Prefabs , and coose the FPSController and add it to the scenea at 0,0,0.

USING THE SCRIPTS
The package contains two scripts - Shoal and ShoalManager.  ShoalManager is the spawner, and needs to be attached to an empty GameObject. The fish will spawn around
this GameObject. Drag your terrain object into the Terrain field in the inspector, drag your fish prefab into the Fish Prefab field (the fish prefab must have the 
Shoal script attached to it in order to work properly), set the water level, the swim limits (the y limit is not important, as it will be determined by the terrain
level and water level) and set Num Fish to be the number of fish you want spawned.  Finally, set the minimum and maximum speed.  You can ignore the other fields, as 
they will be controlled by the script.  You can also ignore the settings in the Shoal script, as they are also controlled within the script.

IF THE ANIMATIONS DON'T WORK
If your are using Mecanim to control the animations, then the animations won't work.  You will need to change the models from using the Legacy rig to the Generic rig.
To do this, select the model you want to change (not the prefab, the original imported model).  In the inspector choose Rig and select Generic as your animation type 
from the drop down list.
