# Game rules — Level 1 (working design)

This is the current playable-design brief. Items marked **Tuning** are intentionally left adjustable until the course can be flown in Unity.

## Goal and course

- Finish one broad, closed lap over open ocean and cross the finish marker near the start.
- Time limit: **60 seconds** for Level 1.
- Route: **six sequential checkpoint gates**, each made from two separated pylons, followed by a distinct finish marker (the seventh course marker).
- Route shape: a large, smooth circuit with long turns and gradual climbs/descents. Avoid tight zigzags. The first course should teach the path without feeling trivial.
- Course readability comes from gate placement and the chase camera. The ocean should extend to the horizon; distant set dressing stays sparse.

## Controls and view

- **A / D:** steer left/right. The aircraft banks gradually and carries momentum; no instant rigid direction snaps.
- **W / S:** climb/descend smoothly.
- **Space:** trigger an available can-powered barrel roll.
- Camera: exterior chase view behind and above the aircraft, angled down enough to see the ocean and the next gate. It may bank subtly with turns. Preserve enough horizon reference for orientation; during barrel rolls, use a controlled camera response rather than an unintentionally disorienting full-camera spin.
- Flight feel: accessible arcade simulation, with smooth acceleration, steering, and altitude response.

## Checkpoints and energy

- The flight-energy bar drains continuously during ordinary flight.
- Crossing a checkpoint gate in the correct order awards **10 points** and replenishes flight energy.
- Touching a pylon causes a soft collision and a short backward rebound. The aircraft is not destroyed by the pylon.
- Contact with or submersion in the ocean ends the run as a crash.
- If the timer expires while the aircraft is still flying, show **TIME OUT** and offer a quick retry.
- **Tuning:** energy drain rate, checkpoint refill amount, rebound distance/strength, and whether the energy drain pauses during a barrel-roll animation.

## Cans and barrel-roll combo

- A collected can is shown as a floating, intentional pickup in an aerial ring between selected checkpoints. It should be visually legible against sky and sea.
- Collecting a can grants energy, points, and a speed boost, and enables a short multi-roll opportunity.
- Pressing **Space** performs one complete longitudinal barrel roll (tonel axial) while the aircraft keeps moving forward.
- Repeated Space presses can chain additional completed rolls. Fast chained rolls build a higher score multiplier; a long delay ends the chain, so one roll by itself remains the base reward.
- Repeated rolls increase speed and lift/altitude, creating a tradeoff: the player can rise above the intended line and spend time recovering the route.
- **Tuning:** can energy/score/speed reward, activation duration, acceptable time between chained rolls, multiplier values, roll duration, and exact altitude response.

## Win, lose, score, and retry

- Win by completing all six checkpoints and crossing the finish marker before the 60-second limit, while still above the ocean.
- Lose by touching/submerging in the ocean or timing out. A pylon bump alone does not end the run.
- Checkpoints contribute 10 points each. Cans and completed chained rolls award additional points and combo multipliers.
- **Tuning:** exact can and stunt scores, score UI/result presentation, and whether hitting a pylon also applies a score penalty.
- On failure, offer a clear, immediate retry. On success, show finish status, elapsed time, checkpoints cleared, stunt/can score, and total score.

## First playable tuning (implemented)

- Flight energy starts at 72/100 and drains at 3.5 points per second. A correct gate restores 26; a can restores 30 and grants one stored boost charge, up to three.
- A can adds 25 points. Each correct gate adds 10 points. Activating a stored boost lasts 3 seconds and increases cruise speed by 35%; energy drain pauses during the boost.
- Space during an active boost queues another roll. Presses within 0.9 seconds build the current combo; each completed 360-degree axial roll awards 10 points times that combo and adds a small upward lift. A new activation requires a stored can charge.
- The level has six gates, 24 m spacing between pylons, checkpoint centers 200–206 m apart (6.25–6.44 seconds at cruise), six aerial can pickups with bright oversized rings, a gold finish gate, and a 60-second limit. Pylon contact subtracts 18 energy and rebounds the aircraft; the run ends at or below 1.5 m above the configured waterline.
- The aircraft root spawns with zero pitch and roll and maintains automatic 32 m/s horizontal forward cruise. **W/S** control altitude independently, while **A/D** turn the heading and bank only the visual model. The 4.8 km square ocean extends past an invisible flight boundary at ±900 m; the water edge stays beyond the camera's 1.4 km far clip and fog horizon. The camera follows from a rear three-quarter angle; **C** selects closer framing. The aircraft FBX mesh is kept intact so it renders reliably; animated propeller separation remains a follow-up item.
- A compact live-data HUD is temporary. Figma implementation, presentation lights/air trails, and Wwise remain for later stages.

## Build order

1. Import approved environment assets and organize their source/license records.
2. Build and inspect the map and route without an aircraft.
3. Integrate the selected aerobatic aircraft, tune its palette, and test the flight feel.
4. Implement gates, energy, pickups, stunts, scoring, HUD, and retry flow.
5. Polish the presentation and integrate Wwise last.
