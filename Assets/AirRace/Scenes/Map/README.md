# Level 01 map scene

`AirRace_Level01_Map.unity` is the first playable course scene. It contains a 4.8 km square of A.U.R.W. ocean, the selected stylized sky, six ordered gates built from the reusable pylon + swim-raft prefab, a separate gold finish gate, six aerial energy-can pickups, the player aircraft, and a three-quarter side-chase camera. The aircraft scale review remains a separate presentation scene.

The route is a broad closed oval. Its checkpoint centers are 200–206 m apart, or about 6.25–6.44 seconds at 32 m/s. The ocean surface spans 4.8 km by 4.8 km. Flight turns inward at an invisible ±900 m boundary, and the water edge sits outside both the camera far clip and fog horizon.

The player aircraft prefab comes from the approved free FBX sample, linked directly to its imported source mesh. This avoids the broken generated-mesh reference that previously made the aircraft invisible. The livery uses the saved metallic blue material, red accents, and yellow details. The propeller is visible but does not rotate yet; its safe separation and animation remain a follow-up. The generic energy can is original low-poly blockout art with a blue body, red band, yellow top, and emissive amber ring. It is intentionally unbranded.

Current controls: W/S climb and descend while horizontal forward movement continues, A/D turn and bank, Space activates a stored can boost and queues a barrel roll, and C switches chase-camera framing. A temporary live-data HUD shows time, gates, score, flight energy, and charges; the Figma UI is deferred to the separately agreed next stage.
