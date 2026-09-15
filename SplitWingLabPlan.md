# Three Floor-Plan Building Prefabs

## Objective

Turn all three supplied two-story floor-plan references into enclosed, walkable graybox building models in Unity 6.3 URP:

- `option-1-central-spine.png` → `Building_Option1_CentralSpine.prefab`
- `option-2-loop-hallway.png` → `Building_Option2_LoopHallway.prefab`
- `option-3-split-wing.png` → `Building_Option3_SplitWing.prefab`

Each prefab is an export-ready architectural model with its own room layout, structural shell, stair core, colliders, and configured baked point-light layout. `SampleScene` remains unchanged; create dedicated option preview/bake scenes and a `BuildingGallery` scene for side-by-side review.

## Shared architecture and scale

- Build one reusable primitive module kit: floor and ceiling slabs, solid and doorway wall segments, exterior walls, flat roof/parapet, and a U-shaped stair module. Use default Unity rendering only: no materials, textures, furniture, network equipment, exterior windows, doors, décor, or other props.
- Use 1 Unity unit = 1 meter, a 3 m structural grid, 0.25 m walls, 3 m floor-to-floor height, 2.8 m clear room ceilings, 1.2 m interior openings, and 1.5 m-wide stairs. Every room on both levels has the same standard height; no double-height rooms, atria, open-to-below spaces, or balcony edges are created.
- Give every model a continuous floor slab on both levels, a complete flat roof, 0.6 m exterior parapet, opaque exterior walls, and structural ceilings. The result is a sealed indoor environment with no sky exposure.
- Retain colliders on floors, walls, roof, and stairs. Each stair has two 1.5 m-wide flights, a mid-landing, 18 risers of approximately 0.167 m, and a clear 2.1 m headroom path between the levels.

## Building variants and prefab delivery

- **Option 1 — Central Spine:** Model the central north–south hallway and stair core; Floor 1 contains Workstation Rooms A/B, Instructor Area, Storage, Server Room, and Network Closet. Floor 2 contains Workstation Rooms C/D/E/F and its Network Closet. Maintain the diagram’s direct, symmetric room access from the spine.
- **Option 2 — Loop Hallway:** Model the enclosed central server/core, continuous main hallway loop, stair core, Workstation Rooms A/B/C, Server Room, Network Closet, Server Room Annex, and Breakout Area on both floors. Preserve the loop as a continuous player route around the core.
- **Option 3 — Split Wing:** Model the long hall between the workstation and infrastructure wings. Floor 1 contains Workstation Labs A/B, Practice Room, Server Room, Network Closet, and Utility Room; Floor 2 contains Observation Area, Practice Room, Core Server Room, Network Closet, plus standard-height enclosed upper rooms over the two bays marked open in the reference. Preserve the diagram’s stair/hall alignment and wing separation.
- Assemble each option under a single named root with `Floor1`, `Floor2`, `Structure`, `Roof`, `Lighting`, and `Spawn` children. Save it as its own prefab in `Assets/Prefabs/Buildings/` for later Unity-package or model export. The shared modules live separately under `Assets/Prefabs/Architecture/`.
- Option 3's stair assembly is split into reusable `StaircaseLower.prefab` and `StaircaseUpper.prefab` flight prefabs, each with nine 2.05 m-wide steps. Preserve the edited `FloorSlab_South` and `StairLanding` dimensions, add a level upper landing, and enclose the second-floor stair opening with a controlled access wall so the stair shaft and surrounding building remain sealed.

## Player and baked lighting

- Add one dependency-free third-person test player to each option preview scene: visible capsule body, `CharacterController`, follow camera, mouse look, WASD movement, walk/sprint, and an interior ground-floor spawn. Use a 2 m controller height, 0.3 m step offset, and 45° slope limit so its scale and movement fit the doors, rooms, and stairs.
- Delete the active `Directional Light`; do not use sun, directional, or other outdoor direct lighting. Set ambient/environment lighting to a low neutral level.
- Give every enclosed room at least one static, baked, shadow-casting neutral-white point light: four in large labs/server/observation spaces, one or two in smaller rooms, regularly spaced lights in each hallway loop or spine, and two in every stair core. Use room-specific 3.5–5 m ranges and baked shadows so light is contained by walls; only doorway spill may cross into adjacent spaces.
- Mark shells static and bake each option in its own preview/bake scene. Unity stores baked lightmaps with the scene, not inside a prefab, so the prefab carries the complete point-light placement/configuration while a future destination scene performs its own bake after import.

## Validation

- Confirm all three prefab roots have complete Floor 1/Floor 2/roof coverage, no open-to-below areas, no directional light, no exposed sky, and no missing colliders or scripts.
- Bake and capture each preview scene; verify every room, hallway, landing, and stair is visibly lit without unintended lighting through solid walls.
- Play-test each option: walk from spawn to every labeled room and between floors, confirming no clipping, stair snags, falls, insufficient clearance, or inaccessible spaces.
- Confirm `SampleScene` remains unchanged, Unity Console is clean, each prefab can be instantiated in `BuildingGallery`, and a Unity CLI compilation/build check succeeds.

## Assumptions

- The three images define room adjacency and circulation, not construction-grade dimensions; the shared scale standard governs final measurements.
- The two unassigned upper bays in Option 3 become enclosed, standard-height empty rooms until their teaching purpose is chosen.
- “Later export” means reusable Unity prefab/model geometry; baked lighting is re-created in the scene that imports the prefab.
