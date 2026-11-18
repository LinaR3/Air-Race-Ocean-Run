# Asset library and credits

Only reviewed, user-approved resources belong in this list. Download/import status must be updated only after it is verified locally in Unity.

## Approved set

| Role | Asset | Source / license | Planned use | Status |
|---|---|---|---|---|
| Water | [A.U.R.W. URP Free](https://assetstore.unity.com/packages/vfx/shaders/a-u-r-w-urp-free-195139) | Unity Asset Store Standard EULA; free extension asset. The creator describes coast foam, refraction, depth effects, and URP compatibility. | Ocean surface and foam where the course meets a coastline. This is a water shader package, not a complete beach model. | Approved; import pending. |
| Sky | [Skybox Sampler: 9 Free Stylized Skies](https://assetstore.unity.com/packages/2d/textures-materials/sky/skybox-sampler-9-free-stylized-skies-421274) | Unity Asset Store Standard EULA; free. Listing states Unity 6 / URP compatibility. | Choose a clear daytime sky preset for the race. | Approved; preset and import pending. |
| Pylon | [Kenney Racing Kit](https://kenney.nl/assets/racing-kit) | CC0, according to Kenney. | Use the approved pylon model as a base and evaluate its scale/shape against the Air Race references. | Approved; download endpoint returned HTTP 403 in this environment; source not imported. |
| Floating base | [Swim Raft](https://polyfork.dev/asset/swim-raft-6aefb5) | CC BY 4.0. Credit Polyfork; share adaptations under the same terms. | Evaluate the low-poly raft as an optional base for a pylon; remove or recolor the wooden deck if it clashes. | Approved; FBX and GLB downloaded locally; import pending. |
| Aircraft | [Aerobatics Aircraft](https://opengameart.org/content/aerobatics-aircraft) | CC BY-SA 3.0. Credit Martins Upitis; adaptations published with the same license. 7,267 triangles; diffuse map and skin template listed. | Integrate after the map is built. Tune a blue/red/yellow palette inspired by the brand, without official logos or copied can/aircraft artwork. | Approved; original Blender archive downloaded locally; extraction/import deferred until map phase. |

## Attribution notes

- Keep the original license files and asset source URLs in `Assets/AirRace/ThirdParty/` alongside imported assets where practical.
- Preserve Polyfork and Martins Upitis attributions in the portfolio credits. If modified versions of those assets are redistributed, honor their share-alike license terms.
- The concept is a fictional portfolio exercise, not an official Red Bull campaign. Use a visible concept disclaimer in a public presentation and avoid official brand artwork/logos.

## Import order

1. Accept the Asset Store license and import water and sky; import the raft for the environment/map phase.
2. Obtain the Kenney pylon model when the source download is available; inspect its dimensions and shape.
3. Build and review the map without an aircraft.
4. Bring in the aircraft only after the map phase; test it against the selected scene and recolor it then.

## Not selected

- AQUAS Lite — Built-In Render Pipeline only; not appropriate for this URP project.
- Free Toon Water — reviewed as an alternate, but A.U.R.W. URP Free is the chosen water package.
- Hawaii Beach House HDRP — pipeline mismatch and not the low-poly style selected.
- A.U.R.W. includes water effects; it does not supply a complete physical beach/coast model. The coastline will be a separate lightweight part of the map design.
