# Asset library and credits

Only reviewed, user-approved resources belong in this list. Download/import status must be updated only after it is verified locally in Unity.

## Approved set

| Role | Asset | Source / license | Planned use | Status |
|---|---|---|---|---|
| Water | [A.U.R.W. URP Free](https://assetstore.unity.com/packages/vfx/shaders/a-u-r-w-urp-free-195139) | Unity Asset Store Standard EULA; free extension asset. The creator describes coast foam, refraction, depth effects, and URP compatibility. | Ocean surface and foam where the course meets a coastline. This is a water shader package, not a complete beach model. | Approved, added to My Assets and imported into the Unity project. Shader still needs visual setup and scene validation. |
| Sky | [Skybox Sampler: 9 Free Stylized Skies](https://assetstore.unity.com/packages/2d/textures-materials/sky/skybox-sampler-9-free-stylized-skies-421274) | Unity Asset Store Standard EULA; free. Listing states Unity 6 / URP compatibility. | Choose a clear daytime sky preset for the race. | Approved, added to My Assets and imported into the Unity project. Golden Hour is the first outdoor preset to evaluate; no coastline or moving sky is included. |
| Pylon | [Kenney Racing Kit](https://kenney.nl/assets/racing-kit) | CC0, according to Kenney. | Use the approved pylon model as a base and evaluate its scale/shape against the Air Race references. | Approved; download endpoint returned HTTP 403 in this environment; source not imported. |
| Floating base | [Swim Raft](https://polyfork.dev/asset/swim-raft-6aefb5) | CC BY 4.0. Credit Polyfork; share adaptations under the same terms. | Evaluate the low-poly raft as an optional base for a pylon; recolor the wood dark navy for the visual comparison. | Approved; FBX and GLB downloaded and imported. Used in the isolated scale review scene. |
| Aircraft | [Aerobatics Aircraft](https://opengameart.org/content/aerobatics-aircraft) | CC BY-SA 3.0. Credit Martins Upitis; adaptations published with the same license. 7,267 triangles; diffuse map and skin template listed. | Integrate after the map is built. Tune a blue/red/yellow palette inspired by the brand, without official logos or copied can/aircraft artwork. | Approved; original Blender archive is downloaded locally, but contains `.blend` and textures only. Unity does not import `.blend` directly, so this copy needs Blender export before Unity can use it. |
| Aircraft candidate | [Low Poly Airfield — Free Sample](https://www.fab.com/listings/43f8170e-349b-46d6-937b-66c7f511d3c1) | Fab Standard EULA accepted for this asset. Creator's included README says free for personal/commercial projects, modification allowed, no attribution required, and no standalone resale/redistribution of source files. | Compare the 8.2 m light high-wing plane against a 25 m Air Race pylon over the approved ocean. | FBX imported into Unity. Scale review scene and capture created. It is a low-poly utility/high-wing shape; review it as a prototype candidate, not yet the final race plane. |

## Aircraft format alternatives reviewed

| Candidate | Format / license | Fit and decision |
|---|---|---|
| [UNDERPOLY: The First Flight (FREE)](https://sketchfab.com/3d-models/underpoly-the-first-flight-free-b3721ea2891a483fbded7bbd9bb2fb1a) | Listing offers individual OBJ/FBX files and states CC BY attribution; approximately 19.3k triangles. | Unity can import the FBX/OBJ without Blender. Its stylized prop aircraft is the closest free direct-import candidate found so far, but is heavier and still needs inspection in the project before selection. Not yet approved or downloaded. |
| [Awesome Cartoon Airplanes](https://assetstore.unity.com/packages/3d/vehicles/air/awesome-cartoon-airplanes-56715) | Unity package; free under Standard Unity Asset Store EULA; approximately 1,760 triangles plus propeller. | Imports directly through Unity, but the listing dates to 2018/Unity 4 and its preview reads as a toy/cartoon airplane. Not recommended as the polished race aircraft without a visual test. |
| [An-2 Airplane Lowpoly](https://opengameart.org/content/an-2-airplane-lowpoly) | FBX; CC0; 1,848 triangles. | Unity-importable without Blender, but an old biplane silhouette that does not match the aerobatic race reference. |

## Attribution notes

- Keep the original license files and asset source URLs in `Assets/AirRace/ThirdParty/` alongside imported assets where practical.
- Preserve Polyfork and Martins Upitis attributions in the portfolio credits. If modified versions of those assets are redistributed, honor their share-alike license terms.
- The concept is a fictional portfolio exercise, not an official Red Bull campaign. Use a visible concept disclaimer in a public presentation and avoid official brand artwork/logos.

## Import order

1. Asset Store license accepted for the approved water and sky packages; both are imported. Swim Raft is imported for the environment/map phase.
2. Obtain the Kenney pylon model when the source download is available; inspect its dimensions and shape.
3. Build and review the map without an aircraft.
4. Compare the free Fab aircraft in `Assets/AirRace/Scenes/Presentation/AircraftScaleReview.unity`; decide whether to keep it for the prototype or continue searching for a more aerobatic silhouette.

## Scale review

`Assets/AirRace/Scenes/Presentation/AircraftScaleReview.unity` is isolated from the Level 01 course. It places the imported aircraft beside a review-only Air Race pylon made from simple tapered mesh bands, with an A.U.R.W. ocean tile and the approved Swim Raft darkened to approximate the floating support in the reference photo. It is a visual scale comparison, not the final course scene or finished pylon asset.

The aircraft FBX's longest model dimension is normalized to the creator's listed 8.2 m. The pylon uses the published Air Race reference dimensions: approximately 25 m high, 5 m at the base and 0.75 m at the tip. The reference pylon dimensions are documented by [Red Bull](https://www.redbull.com/de-de/so-funktionieren-die-red-bull-air-race-pylonen-ucar) and [Box Repsol](https://www.boxrepsol.com/en/red-bull-air-race-en/the-airgators-guardians-of-the-pylons-at-the-red-bull-air-race/).

## Not selected

- AQUAS Lite — Built-In Render Pipeline only; not appropriate for this URP project.
- Free Toon Water — reviewed as an alternate, but A.U.R.W. URP Free is the chosen water package.
- Hawaii Beach House HDRP — pipeline mismatch and not the low-poly style selected.
- A.U.R.W. includes water effects; it does not supply a complete physical beach/coast model. The coastline will be a separate lightweight part of the map design.
