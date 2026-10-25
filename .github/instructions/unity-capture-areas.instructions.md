---
description: Capture and delivery volume rules for Resource War Unity scripts.
applyTo: 'Assets/Scripts/**/*.cs'
---

Capture and delivery objects often inherit a large visual-model scale. Collider dimensions are local-space values: when adding or changing a sphere/box collider, convert the intended world-space radius by the transform's lossy scale or use a world-space distance check. Do not add broad colliders directly to scaled factory capture objects; this can cause unrelated structures to remain permanently contested.

Vehicles have multiple wheel/body colliders. Capture and interaction zones must track entry/exit counts per vehicle root, not treat a single collider exit as the vehicle leaving the zone.
