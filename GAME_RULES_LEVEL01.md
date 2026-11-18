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

## Build order

1. Import approved environment assets and organize their source/license records.
2. Build and inspect the map and route without an aircraft.
3. Integrate the selected aerobatic aircraft, tune its palette, and test the flight feel.
4. Implement gates, energy, pickups, stunts, scoring, HUD, and retry flow.
5. Polish the presentation and integrate Wwise last.
