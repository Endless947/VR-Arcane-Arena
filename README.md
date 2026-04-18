# VR Arcane Arena

> **See the Algorithms. Feel the Magic.**

A VR magic combat game for Meta Quest 2 where every game mechanic is powered by a real advanced data structure from **CS2308: Data Structures-II**. The player stands in a circular arena, survives five waves of enemies, and casts spells by inputting gesture token sequences on the controller — while watching all four data structures execute visually in 3D space in real time.

Built solo as both a **CS2308 course project** and a **hackathon submission** for Tesseract '26, Open Innovation Track, SIG Reality Spectra, VIT Pune (April 4–5, 2026 — 24-hour offline hackathon).

---

## Table of Contents

- [What Makes This Different](#what-makes-this-different)
- [Academic Context](#academic-context)
- [The Four Data Structures](#the-four-data-structures)
  - [Octree — Spatial Partitioning](#1-octree--spatial-partitioning-unit-5)
  - [Trie / DAWG — Spell Casting](#2-trie--dawg--spell-casting-unit-3)
  - [Fibonacci Heap — Threat System](#3-fibonacci-heap--threat-system-unit-2)
  - [Skip List — Cooldown Manager](#4-skip-list--cooldown-manager-unit-4)
- [Spell System](#spell-system)
- [Enemy Types](#enemy-types)
- [Wave System](#wave-system)
- [Debug Overlay System](#debug-overlay-system)
- [Design Decisions](#design-decisions)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Setup & Build](#setup--build)
- [How to Play](#how-to-play)
- [References](#references)

---

## What Makes This Different

Most game projects use data structures invisibly in the background. VR Arcane Arena makes them the spectacle. Every mechanic is a direct, functional application of a data structure from the course syllabus — and every data structure is rendered live as a glowing 3D debug overlay that the player can see and understand while playing.

| What you see in VR | What's running underneath |
|---|---|
| Purple wireframe boxes subdividing around enemies | Octree partitioning 3D space in real time |
| Gold path lighting up on the Trie panel as you press buttons | Trie prefix traversal, node by node |
| White ring under the Boss, gradient rings under others | Fibonacci Heap max-priority ordering |
| Colored bars draining on the right panel after casting | Skip List sorted by cooldown expiry timestamp |

The core idea: a student who plays this game for five minutes develops an intuition for these structures that a static 2D textbook diagram cannot provide.

---

## Academic Context

| Field | Details |
|---|---|
| Course | CS2308: Data Structures-II, VIT Pune |
| Academic Year | 2025-26 |
| Units Covered | Unit 2 (Heaps), Unit 3 (String DS), Unit 4 (Randomized DS), Unit 5 (Spatial DS) |
| Reference Project | #16 — Building a 3D Game World Using Octrees |
| Hackathon | Tesseract '26, Open Innovation Track |
| Organizer | SIG Reality Spectra (VR/AR club), VIT Pune |
| SDG Alignment | SDG 4: Quality Education |

### Course Outcomes Addressed

| CO | Statement | How This Project Addresses It |
|---|---|---|
| CO2 | Model real-world problems with priority queues and heap data structures | Fibonacci Heap drives the live enemy threat-scoring and auto-targeting system |
| CO3 | Construct and evaluate string processing structures for real-world applications | Trie/DAWG drives the entire gesture-based spell casting input pipeline |
| CO4 | Apply randomized data structures for optimised performance | Skip List manages all spell cooldowns, sorted by expiry timestamp |
| CO5 | Model and query spatial and multidimensional data effectively | Octree partitions the 3D arena and handles all AoE sphere-cast queries |

---

## The Four Data Structures

### 1. Octree — Spatial Partitioning (Unit 5)

**Game mechanic:** 3D arena spatial partitioning and AoE spell hit detection.

The Octree root is centered at world origin with `halfSize = 25`. It subdivides whenever a node holds more than `maxEntitiesPerNode` (default 4) enemies. Every enemy is dynamically inserted and removed as it moves each frame. When an AoE spell fires, a `QuerySphere()` call walks the Octree to return only the enemies within blast radius — no brute-force iteration over the entire enemy list.

| Property | Value |
|---|---|
| Query method | `QuerySphere()` |
| Time complexity | O(log n + k) per sphere query |
| Insert / Delete | O(log n) per enemy per frame |
| Visual | Purple LineRenderer wireframe boxes in world space. Brighter = more enemies in node. White = at capacity. |

**Why Octree over KD-Tree?** A KD-Tree requires a full O(n log n) rebuild whenever any point moves. Enemies move every frame — that is an O(n log n) rebuild 72 times per second. The Octree handles dynamic insert and delete natively in O(log n) per enemy, making it the correct structure for a real-time game with continuously moving entities.

---

### 2. Trie / DAWG — Spell Casting (Unit 3)

**Game mechanic:** Gesture-based spell recognition via controller button sequences.

Eight spells are loaded into the Trie at startup via `LoadDefaultSpells()`. Controller buttons map to four tokens — F, P, O, S — and each button press advances the current traversal path one node deeper. The Trie visualizer updates every node's color in real time: gold for the active traversal path, white for reachable next tokens, grey for unreachable branches, and a red flash for an invalid sequence attempt. When the traversal reaches a terminal node, the corresponding spell fires automatically.

| Property | Value |
|---|---|
| Recognition complexity | O(L) where L is the gesture sequence length |
| Tokens | F (Fist), P (Point), O (Open Palm), S (Spread) |
| Visual | Node graph bottom-left of view. Gold = active path. White = reachable. Grey = unreachable. Red flash = invalid input. |

**Why Trie over HashMap?** A HashMap requires an exact key match — it can only confirm a completed sequence, not partial progress. The Trie gives prefix traversal and live autocomplete: the visualizer can show which spells are still reachable after every single token, not just at the end. This is what makes the live visualization meaningful.

---

### 3. Fibonacci Heap — Threat System (Unit 2)

**Game mechanic:** Enemy threat scoring, auto-prioritization, and spell targeting.

The Fibonacci Heap is a max-heap keyed by each enemy's threat score. Threat is calculated as `baseThreat + (1 / distance)` — base threat ensures type ordering (Boss = 100, Archer = 10, Goblin = 1) while the distance tiebreaker means the closest enemy of a given type is always prioritized over a farther one. The heap is updated every 0.1 seconds. The Fireball spell always targets the heap's maximum — the highest threat enemy — without scanning the full enemy list.

| Property | Value |
|---|---|
| Key operation | O(1) amortized decrease-key |
| Update rate | Every 0.1 seconds |
| Updates per second | ~7,200 at 100 enemies × 72 FPS |
| Visual | Threat rings under every enemy. White ring = current highest-threat target. Red → orange → yellow → green → blue gradient for lower-threat enemies. Rings reorder live as decrease-key fires. |

**Why Fibonacci Heap over Binary Heap?** A Binary Heap's decrease-key is O(log n). A Fibonacci Heap's is O(1) amortized. At 100 enemies × 72 FPS, that is over 7,200 priority updates per second — the difference between O(1) and O(log n) per update is the difference between the game running at full framerate and failing to maintain 72 FPS on standalone Quest hardware.

---

### 4. Skip List — Cooldown Manager (Unit 4)

**Game mechanic:** Spell cooldown queue management, sorted by expiry timestamp.

Every time a spell is cast, an entry is inserted into the Skip List with an expiry timestamp of `Time.time + cooldownDuration`. The Skip List maintains this sorted order in O(log n) expected time per insertion. O(1) peek at the head always gives the next cooldown to expire. The UI reads the list every frame to update the cooldown bars.

| Property | Value |
|---|---|
| Insertion | O(log n) expected |
| Peek next expiry | O(1) |
| Visual | Colored progress bars bottom-right of view. Each bar drains during cooldown. Dims when the spell is ready again. Sorted by expiry timestamp. |

**Why Skip List over a sorted array?** A sorted array requires O(n) shifting on every insertion. The Skip List gives O(log n) expected insertion with O(1) peek — and it directly demonstrates the Unit 4 randomized data structure, which a sorted array would not.

---

## Spell System

Controller buttons map to Trie tokens. Press tokens in sequence — the Trie panel lights up the active path. Complete a valid sequence and the spell fires automatically.

| Button | Token | Gesture Name |
|---|---|---|
| A (right) | F | Fist |
| B (right) | P | Point |
| X (left) | O | Open Palm |
| Y (left) | S | Spread |

### Spell List

| Sequence | Tokens | Spell | Effect | Cooldown |
|---|---|---|---|---|
| Fist → Point | FP | Fireball | Tracking projectile → highest threat enemy | 3s |
| Open × 2 → Spread | OOS | Blizzard | AoE damage radius 8f | 8s |
| Point × 2 → Fist | PPF | Lightning Bolt | Chain hits 3 enemies | 5s |
| Spread → Open | SO | Arcane Shield | Absorbs next 3 hits | 10s |
| Fist × 3 | FFF | Meteor Strike | Massive AoE radius 12f | 20s |
| Open → Point → Spread | OPS | Gravity Well | Pulls enemies radius 15f | 12s |
| Point → Open | PO | Frost Nova | AoE damage radius 6f | 6s |
| Spread → Fist × 2 | SFF | Void Blast | Massive damage radius 20f | 15s |

> **Editor fallbacks:** Space = Fireball, keys 1–7 = other spells in sequence. Useful for testing without a headset.

---

## Enemy Types

| Type | Speed | Damage | Health | Base Threat | Color |
|---|---|---|---|---|---|
| Goblin | 0.8 | 5 | 50 | 1 | Red sphere |
| GoblinArcher | 1.0 | 15 | 100 | 10 | Orange sphere |
| GoblinBoss | 0.6 | 50 | 150 | 100 | Dark purple sphere |

**Spawn formation:** Boss at back center → 2 Archers flanking → Goblins spread in front. All enemies drop from Y=12, land at Y=0, pause 2.5 seconds, then march. Formation always faces the player's direction.

---

## Wave System

| Parameter | Value |
|---|---|
| Total waves | 5 |
| First wave delay | 5 seconds |
| Time between waves | 15 seconds |
| Enemy count formula | `10 + (wave − 1) × 5` |

| Wave | Enemy Count |
|---|---|
| 1 | 10 |
| 2 | 15 |
| 3 | 20 |
| 4 | 25 |
| 5 | 30 |

---

## Debug Overlay System

All four data structures are visible simultaneously during gameplay. This is the core educational feature of the project.

**Octree** — Purple wireframe boxes in world space. Brighter colour = more enemies packed into that node. White = at capacity. Boxes shrink to cluster around enemy groups. Sphere-cast visualization fires on every AoE spell.

**Trie** — Node graph bottom-left of view. Gold = active traversal path. White = reachable next tokens from the current position. Grey = unreachable. Red flash = invalid sequence entered.

**Fibonacci Heap** — Threat rings under every enemy. White ring = current highest-threat target (Fireball will hit this enemy). Red → orange → yellow → green → blue gradient for all lower-threat enemies. Rings reorder live as enemy distances and positions change.

**Skip List** — Colored progress bars bottom-right of view. Each bar drains during its spell's cooldown period. Dims when the spell is ready again. Bars are sorted by expiry timestamp — the next cooldown to expire is always at the top.

---

## Design Decisions

| Decision | Reasoning |
|---|---|
| Fibonacci Heap over Binary Heap | O(1) amortized decrease-key vs O(log n). At 100 enemies × 72 FPS = 7,200 priority updates/sec, this is a meaningful performance difference on standalone Quest hardware. |
| Octree over KD-Tree | KD-Tree requires O(n log n) full rebuild every frame for moving enemies. Octree handles dynamic insert/delete in O(log n) per enemy. |
| Skip List over sorted array | O(log n) insertion vs O(n) shifting. O(1) peek at the next expiring cooldown. Also directly demonstrates the Unit 4 randomized DS. |
| Trie over HashMap | HashMap requires exact key match. Trie gives prefix traversal and live autocomplete — the visualizer can show reachable spells after every token, not just at completion. |
| OpenXR over Oculus XR Plugin | The Oculus plugin caused a frozen state on the headset (scripts hanging on OVRInput calls that never initialized). OpenXR with XR Interaction Toolkit is the stable path for Quest 2 in 2025-26. |
| Controller buttons as Trie tokens | Hand tracking requires specific OpenXR feature flags and environment conditions. In a 24-hour hackathon demo, controllers are 100% reliable. Buttons A/B/X/Y map to tokens F/P/O/S — the Trie traversal, prefix matching, and visualizer all function identically. |
| Unlit/Color shader only | Legacy particle shaders are stripped from the Android APK build. Unlit/Color is always included by Unity in every build, so all visuals — wireframes, rings, projectiles — are guaranteed to render on device. |
| Flat 2D distance for player contact | The player camera sits at ~1.6m height. A 3D distance check from camera to ground-level enemies never triggers correctly. Flat 2D distance (ignoring Y) is the correct approach. |
| World-space canvases parented to camera | Screen Space Overlay is completely invisible in VR headsets. All UI canvases use World Space and are parented to the Main Camera. |

---

## Tech Stack

| Field | Value |
|---|---|
| Engine | Unity 2022.3 LTS |
| Language | C# |
| Platform | Meta Quest 2 |
| XR Plugin | OpenXR |
| XR Template | VR Core (XR Interaction Toolkit) |
| Hand Package | com.unity.xr.hands |
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| Min API | 32 |
| Target API | Auto (Project Setting) |
| Input Handling | Both (legacy + new Input System) |
| Tracking Origin | Floor |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── DataStructures/       ← Pure C#, zero Unity dependency
│   │   ├── Octree.cs
│   │   ├── SpellTrie.cs
│   │   ├── FibonacciHeap.cs
│   │   └── CooldownSkipList.cs
│   ├── Managers/
│   │   ├── OctreeManager.cs
│   │   ├── ThreatManager.cs
│   │   ├── CooldownTracker.cs
│   │   └── GestureDetector.cs
│   ├── Game/
│   │   ├── EnemyStats.cs
│   │   ├── Enemy.cs
│   │   ├── EnemySpawner.cs
│   │   ├── SpellController.cs
│   │   ├── SpellEffects.cs
│   │   ├── SpellProjectile.cs
│   │   ├── PlayerHealth.cs
│   │   └── GameManager.cs
│   └── UI/
│       ├── TrieVisualizer.cs
│       ├── CooldownStripUI.cs
│       └── GameHUD.cs
├── Scenes/
│   └── vrarena.unity
├── Prefabs/
│   ├── GoblinPrefab.prefab
│   ├── Archer.prefab
│   └── boss.prefab
└── Materials/
```

The `DataStructures/` folder contains pure C# implementations with zero Unity dependency. All four structures can be unit tested independently of the engine.

---

## Setup & Build

### Requirements

- Unity 2022.3 LTS
- Meta Quest 2 with Developer Mode enabled
- Unity Package: `com.unity.xr.hands`
- Unity Package: XR Interaction Toolkit 3.x

### Steps

1. Clone the repo and open in Unity 2022.3 LTS.
2. Install packages via Package Manager:
   - `com.unity.xr.hands`
   - Confirm XR Interaction Toolkit is present.
3. **Edit → Project Settings → XR Plug-in Management → Android tab:**
   - Tick `OpenXR`
   - Under OpenXR → Interaction Profiles: add `Oculus Touch Controller Profile` and `Meta Hand Tracking Aim Profile`
   - Under Features: tick `Hand Tracking Subsystem` and `Meta Quest Support`
4. **Edit → Project Settings → Player → Other Settings → Active Input Handling → `Both`**
5. **File → Build Settings** → switch platform to Android → add `vrarena.unity`
6. Connect Quest 2 via USB → Build and Run.

### First Run on Headset

- The app will appear under **App Library → Unknown Sources**
- Accept the hand tracking / controller prompt
- First wave spawns 5 seconds after launch

### VS Code Setup (Optional)

- The repo includes workspace config in `.vscode/` for contributors using VS Code.
- Recommended extension: Visual Studio Tools for Unity (`visualstudiotoolsforunity.vstuc`).
- Debug profile: `Attach to Unity` is preconfigured in `.vscode/launch.json`.
- Workspace settings hide Unity-generated files and set `VR-Arcane-Arena-Hackathon.sln` as the default solution.

---

## How to Play

1. Look around the arena — purple wireframe boxes are the Octree visualizing spatial partitioning in real time.
2. Enemies spawn in formation and march toward you. Check the threat rings — the white ring marks the enemy the Fibonacci Heap has ranked as highest threat.
3. Cast spells by pressing controller buttons in sequence:
   - **A → B** inputs tokens `F → P` → **Fireball** fires at the highest-threat enemy
   - **X → X → Y** inputs tokens `O → O → S` → **Blizzard** AoE
   - Watch the **left Trie panel** light up gold as each token registers
4. After casting, the **right cooldown panel** shows each spell's bar draining during cooldown (Skip List), dimming when it's ready again.
5. Survive all 5 waves to win.

---

## References

1. Sartaj Sahni, Dinesh P. Mehta — *Handbook of Data Structures and Applications*, 2nd Ed.
2. T. Cormen, R. Rivest, C. Stein, C. Leiserson — *Introduction to Algorithms*, 2nd Ed., PHI
3. Peter Brass — *Advanced Data Structures*, Cambridge University Press
4. Meta XR SDK Documentation — developer.oculus.com
5. Unity XR Interaction Toolkit Docs — docs.unity3d.com
6. Unity XR Hands Package — com.unity.xr.hands

---

*Built by Team Threshold (Siddharth Deulkar) — CS2308: Data Structures-II, VIT Pune, AY 2025-26*
*Tesseract '26 · Open Innovation Track · SIG Reality Spectra*
