# Aircraft scale review

Open `AircraftScaleReview.unity` to inspect the imported free Fab aircraft between two separated Air Race pylon references above the A.U.R.W. water surface.

This is a separate presentation/review scene. It does not modify `AirRace_Level01_Map.unity` and it is not a finished game level. The aircraft's longest dimension is scaled to the creator's listed 8.2 m.

The shared pylon assembly is also saved as `Assets/AirRace/Art/Models/Pylons/AirRace_Pylon_SwimRaft.prefab`. It combines the approved Swim Raft with a tapered, striped race pylon and colliders. The current review proportions use the reference dimensions of 25 m height, 5 m base, and 0.75 m tip. Treat the pylon body as a blockout that can be swapped for a higher fidelity free asset later.

`Assets/AirRace/Documentation/Previews/aircraft_scale_review.png` is a rendered camera preview. The scene uses a simple clear daytime sky for the comparison; the approved Skybox Sampler package remains available for choosing the final sky later.

The airplane imports and renders in Unity. The review livery remaps its original materials to a blue body, red trim, and yellow details, without the original white dominating. Its low-poly, high-wing utility-aircraft silhouette is visibly different from a purpose-built aerobatic race plane, so keep it as a free prototype candidate until the visual direction is approved. The propeller is part of its static mesh and is not yet animated.
