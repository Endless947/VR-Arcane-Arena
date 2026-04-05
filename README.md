# VR Arcane Arena

> **See the Algorithms. Feel the Magic.**

A VR magic combat game for Meta Quest 2 where every game mechanic is powered by a real advanced data structure from CS2308: Data Structures-II. The player stands in a circular arena, survives waves of enemies, and casts spells by inputting gesture token sequences on the controller — while watching all four data structures execute visually in 3D space in real time.

Built solo by Team Threshold for **Tesseract '26, Open Innovation Track, SIG Reality Spectra, VIT Pune** — April 4-5, 2026 (24-hour offline hackathon).

---

## What Makes This Different

Most game projects use data structures invisibly in the background. VR Arcane Arena makes them the spectacle.

| What you see | What's running |
|---|---|
| Purple wireframe boxes around enemies | Octree subdividing in real time |
| Gold path lighting up on the Trie panel as you press buttons | Trie prefix traversal live |
| White ring under the Boss, gradient rings under others | Fibonacci Heap max-priority ordering |
| Colored bars draining on the right panel after casting | Skip List sorted by expiry timestamp |

---

## Four Data Structures

| Data Structure | Game Mechanic |
|---|---|
| **Octree** | 3D arena spatial partitioning + AoE spell hit detection |
| **Trie / DAWG** | Spell casting via controller token sequences |
| **Fibonacci Heap** | Enemy threat scoring + auto-prioritization + targeting |
| **Skip List** | Spell cooldown queue management |

---

## Spell System

Controller buttons map to Trie tokens. Press tokens in sequence — the Trie panel lights up the active path. Complete a sequence and the spell fires automatically.

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
| Open → Open → Spread | OOS | Blizzard | AoE damage radius 8f | 8s |
| Point → Point → Fist | PPF | Lightning Bolt | Chain hits 3 enemies | 5s |
| Spread → Open | SO | Arcane Shield | Absorbs next 3 hits | 10s |
| Fist × 3 | FFF | Meteor Strike | Massive AoE radius 12f | 20s |
| Open → Point → Spread | OPS | Gravity Well | Pulls enemies radius 15f | 12s |
| Point → Open | PO | Frost Nova | AoE damage radius 6f | 6s |
| Spread → Fist × 2 | SFF | Void Blast | Massive damage radius 20f | 15s |

---

## Enemy Types

| Type | Speed | Damage | Health | Threat | Color |
|---|---|---|---|---|---|
| Goblin | 0.8 | 5 | 50 | 1 | Red sphere |
| GoblinArcher | 1.0 | 15 | 100 | 10 | Orange sphere |
| GoblinBoss | 0.6 | 50 | 150 | 100 | Dark purple sphere |

Spawn formation: Boss at back center → 2 Archers flanking → Goblins spread in front. All enemies drop from Y=12 and march after a 2.5s pause. Formation always faces the player.

---

## Wave System

- 5 waves total
- First wave spawns after **5 seconds**
- Subsequent waves: 15 seconds between waves
- Enemy count: `10 + (wave - 1) × 5` → Wave 1=10, Wave 2=15 ... Wave 5=30

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
| Min API | 29 / Target API 34 |
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
│   └── ArenaScene.unity
├── Prefabs/
│   ├── GoblinPrefab
│   ├── ArcherPrefab
│   └── BossPrefab
└── Materials/
```

The `DataStructures/` folder is completely independent of Unity — pure C# classes that can be unit tested without any engine dependency.

---

## Setup & Build

### Requirements
- Unity 2022.3 LTS
- Meta Quest 2 with Developer Mode enabled
- Unity Package: `com.unity.xr.hands`
- Unity Package: `XR Interaction Toolkit 3.x`

### Steps

1. Clone the repo and open in Unity 2022.3 LTS
2. Install packages via Package Manager:
   - `com.unity.xr.hands`
   - Confirm XR Interaction Toolkit is present
3. Edit → Project Settings → XR Plug-in Management → Android tab:
   - Tick `OpenXR`
   - Under OpenXR → Interaction Profiles: add `Oculus Touch Controller Profile` and `Meta Hand Tracking Aim Profile`
   - Under Features: tick `Hand Tracking Subsystem` and `Meta Quest Support`
4. Edit → Project Settings → Player → Other Settings → Active Input Handling → `Both`
5. File → Build Settings → switch platform to Android → add `ArenaScene`
6. Connect Quest 2 via USB → Build and Run

### First Run on Headset
- App will be in **App Library → Unknown Sources**
- Accept hand tracking / controller prompt
- First wave spawns after 5 seconds

---

## How to Play

1. Look around the arena — purple wireframe boxes are the Octree visualizing spatial partitioning
2. Enemies spawn in formation and march toward you
3. Check the **right panel** — white ring under an enemy means the Fibonacci Heap has it as highest threat. Fireball always targets it first.
4. Cast spells by pressing controller buttons as token sequences:
   - Press **A** then **B** → `F` then `P` → **Fireball** fires at the Boss
   - Watch the **left Trie panel** light up gold as each token registers
5. After casting, watch the **right cooldown panel** — bars drain during cooldown, dim when ready (Skip List)
6. Survive all 5 waves to win

---

## Debug Overlays

All four data structures are visible during gameplay:

- **Octree** — Purple wireframe boxes in world space. Brighter = more enemies in node. White = at capacity.
- **Trie** — Node graph on bottom-left of view. Gold = active traversal path. White = reachable next tokens. Grey = unreachable. Red flash = invalid sequence.
- **Fibonacci Heap** — Threat rings under every enemy. White ring = current highest threat target. Red → orange → yellow → green → blue gradient for lower threats.
- **Skip List** — Colored progress bars bottom-right. Each bar drains during cooldown. Dims when spell is ready again.

---

## Design Decisions

**Fibonacci Heap over Binary Heap** — O(1) amortized decrease-key vs O(log n). At 100 enemies × 72 FPS = 7,200 priority updates/sec, the difference is significant.

**Octree over KD-Tree** — KD-Tree requires O(n log n) full rebuild every frame for moving enemies. Octree handles dynamic insert/delete in O(log n) per enemy.

**Skip List over sorted array** — O(log n) insertion vs O(n) shifting. O(1) peek at the next expiring cooldown.

**Trie over HashMap** — HashMap requires exact key match. Trie gives prefix traversal and live autocomplete so the visualizer can show reachable spells as each token is entered, not just the final result.

**OpenXR over Oculus XR Plugin** — The Oculus plugin caused a frozen state on the headset (scripts hanging on OVRInput calls that never initialized). OpenXR with XR Interaction Toolkit is the correct stable path for Quest 2 in 2025-26.

**Controller buttons as Trie tokens** — Hand tracking requires specific OpenXR conditions. In a 24-hour hackathon demo, controllers are 100% reliable. Buttons A/B/X/Y map directly to tokens F/P/O/S — the Trie traversal, prefix matching, and visualizer all function identically to hand gestures.

**Unlit/Color shader only** — Legacy particle shaders get stripped from the Android APK build. Unlit/Color is always included by Unity in every build.

---

## Academic Context

| Field | Details |
|---|---|
| Course | CS2308 Data Structures-II, VIT Pune |
| Academic Year | 2025-26 |
| Units Covered | Unit 2 (Heaps), Unit 3 (String DS), Unit 4 (Randomized DS), Unit 5 (Spatial DS) |
| Reference Project | #16 — Building a 3D Game World Using Octrees |
| Hackathon | Tesseract '26, Open Innovation Track |
| Organizer | SIG Reality Spectra (VR/AR club), VIT Pune |

---

## References

1. Sartaj Sahni, Dinesh P. Mehta — *Handbook of Data Structures and Applications*, 2nd Ed.
2. T. Cormen, R. Rivest, C. Stein, C. Leiserson — *Introduction to Algorithms*, 2nd Ed., PHI
3. Peter Brass — *Advanced Data Structures*, Cambridge University Press
4. Meta XR SDK Documentation — developer.oculus.com
5. Unity XR Interaction Toolkit Docs — docs.unity3d.com
6. Unity XR Hands Package — com.unity.xr.hands

---

*Built by Team Threshold — Tesseract '26, Open Innovation Track, SIG Reality Spectra, VIT Pune*
