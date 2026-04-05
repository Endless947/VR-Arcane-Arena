# VR Arcane Arena

> **See the Algorithms. Feel the Magic.**

A Meta Quest VR magic combat game where every mechanic is powered by a real advanced data structure — and you can *see* them running live in 3D space around you.

---

## What Is This?

VR Arcane Arena is an immersive virtual reality game built for Meta Quest. You stand in a glowing arena, cast spells with real hand gestures, and survive waves of incoming enemies. Every major system is driven by a well-studied algorithm, visualised as world-space overlays and a screen-space HUD.

Built for:
- 🏆 **Tesseract '26 Hackathon** — Open Innovation Track — SIG Reality Spectra, VIT Pune
---

## The Four Data Structures

| Game Mechanic | Data Structure | Complexity Highlight |
|---|---|---|
| 3D arena spatial partitioning + AoE hit detection | **Octree** |  O(log n + k) sphere queries |
| Spell casting via hand gesture sequences | **Trie** |  | O(L) spell recognition |
| Enemy threat scoring + auto-targeting | **Fibonacci Heap** |  O(1) amortized decrease-key |
| Spell cooldown queue management | **Skip List** |  O(log n) insertion |

---

## Features

### 🌐 Arena Spatial Management (Octree)
The arena is divided into a dynamic 3D Octree visible as runtime line-overlay boxes in the game world and headset view. As enemies cluster together, nodes subdivide into 8 children in real time. When enemies spread out or die, nodes merge back. AoE spells use sphere queries against the Octree achieving O(log n + k) performance instead of O(n) brute force.

### 🔮 Gesture-Based Spell Casting (Trie)
Make real hand signs in VR to chain spell sequences. A Trie resolves your gesture sequence to the correct spell in O(L) time. A floating world-space panel shows the Trie as a live node graph — nodes labelled with gesture names (ROOT / Fist / Point / Open / Spread), lighting up gold on the active path, white on reachable nodes, grey on dead ends. Spell names are shown in gold below terminal nodes.

### 🎯 Fibonacci Heap Auto-Targeting
Every frame, enemy threat scores update via O(1) amortized decrease-key operations. The Fibonacci Heap always knows the highest threat enemy. Projectile-based offensive spells target that enemy. Threat rings show heap priority — white ring = highest threat (Boss), red → orange → yellow → green → blue for lower ranks. When the Boss dies the heap recalculates and the nearest Archer becomes the new target.

### ⏱️ Spell Cooldown System (Skip List)
After casting, spells enter a Skip List ordered by expiry time. O(1) peek always shows which spell refreshes next. A floating world-space strip displays colour-coded progress bars draining and refilling per spell.

### 🏟️ Wave System + Game States
Survive 5 waves of enemies. The first wave starts after a 15-second delay. Wave 1 spawns 10 enemies, and each later wave adds 5 more enemies than the last. Score points per kill and per wave cleared. The HUD shows current wave, score, and player health bar. Game Over and You Win screens appear on completion.

---

## Spell List

| Gesture Sequence | Tokens | Spell | Effect |
|---|---|---|---|
| Fist → Point | F → P | Fireball | Tracking projectile to highest threat enemy |
| Open → Open → Spread | O → O → S | Blizzard | AoE damage radius 8, projectile to target |
| Point → Point → Fist | P → P → F | Lightning Bolt | Hits 3 enemies, projectile to target |
| Spread → Open | S → O | Arcane Shield | Absorbs next hits |
| Fist → Fist → Fist | F → F → F | Meteor Strike | Massive AoE radius 12, projectile to target |
| Open → Point → Spread | O → P → S | Gravity Well | Pulls all enemies toward player |
| Point → Open | P → O | Frost Nova | AoE damage radius 6, projectile to target |
| Spread → Fist → Fist | S → F → F | Void Blast | Massive damage radius 20, projectile to target |

---

## Enemy Types

| Type | Speed | Damage | Health | Base Threat |
|---|---|---|---|---|
| Goblin | 0.8 | 5 | 50 | 1 |
| Goblin Archer | 1.0 | 15 | 100 | 10 |
| Goblin Boss | 0.6 | 50 | 150 | 100 |

---

## Debug Overlay System

Every data-structure-driven system is visible during gameplay:

- **Runtime line-overlay boxes** — Octree nodes subdividing in real time as enemies cluster
- **Floating world-space panel** — Trie node graph with gesture labels, gold active path, white reachable nodes, and gold spell names at terminal nodes
- **Threat rings** — Fibonacci Heap priority shown as colour rings under every enemy
- **Floating world-space strip** — Skip List cooldown bars draining and refilling per spell
- **Screen-space HUD** — Current wave, score, health, Game Over, and You Win state

---

## Keyboard Controls (Editor Testing)

| Key | Spell |
|---|---|
| Space | Fireball |
| 1 | Blizzard |
| 2 | Lightning Bolt |
| 3 | Arcane Shield |
| 4 | Meteor Strike |
| 5 | Gravity Well |
| 6 | Frost Nova |
| 7 | Void Blast |

---

## Tech Stack

| Category | Tool |
|---|---|
| Game Engine | Unity 2022.3 LTS |
| VR SDK | Meta XR Core SDK + Interaction SDK |
| Language | C# |
| Target Hardware | Meta Quest 2 / 3 |
| DS Visualisation | LineRenderer + uGUI (world-space and screen-space canvases) |
| Dev Testing | Meta XR Simulator (no headset needed) |
| Version Control | Git + GitHub |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── DataStructures/        # Pure C# DS implementations
│   │   ├── Octree.cs
│   │   ├── SpellTrie.cs
│   │   ├── FibonacciHeap.cs
│   │   └── CooldownSkipList.cs
│   ├── Managers/              # Unity MonoBehaviour wrappers
│   │   ├── OctreeManager.cs
│   │   ├── ThreatManager.cs
│   │   ├── CooldownTracker.cs
│   │   └── GestureDetector.cs
│   ├── Game/                  # Game logic
│   │   ├── Enemy.cs
│   │   ├── EnemyStats.cs
│   │   ├── EnemySpawner.cs
│   │   ├── SpellController.cs
│   │   ├── SpellEffects.cs
│   │   ├── SpellProjectile.cs
│   │   ├── PlayerHealth.cs
│   │   └── GameManager.cs
│   └── UI/                    # Wrist UI panels + HUD
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

---

## Data Structure Complexity Reference

### Octree
```
Insert:        O(log n) average
Remove:        O(log n) average
Update:        O(log n) average
Sphere Query:  O(log n + k)   k = results returned
Space:         O(n)
```

### Trie
```
Insert spell:      O(L)   L = gesture sequence length
Lookup/Traverse:   O(L)
Autocomplete:      O(S)   S = reachable spells
```

### Fibonacci Heap
```
Insert:        O(1) amortized
Find-Max:      O(1)
Increase-Key:  O(1) amortized
Extract-Max:   O(log n) amortized
```

### Skip List
```
Insert:     O(log n) expected
Peek-Min:   O(1)
Space:      O(n log n) expected
```

---

## Design Decisions

**Why Fibonacci Heap over Binary Heap?**
Binary Heap requires O(log n) decrease-key. With enemies updating threat scores every frame at 72 FPS, Fibonacci Heap's O(1) amortized decrease-key makes per-frame updates tractable.

**Why Octree over KD-Tree?**
KD-Trees require O(n log n) reconstruction after dynamic updates. Enemies move every frame making KD-Tree rebuilding impractical. Octrees support O(log n) dynamic insertion and deletion natively.

**Why Skip List over sorted array?**
Sorted array requires O(n) insertion due to element shifting. Skip List achieves O(log n) insertion while maintaining O(1) peek at minimum — directly demonstrates Unit 4 randomized DS requirements.

**Why Unlit/Color shader on prefabs?**
Runtime material.color changes require Unlit shader. Standard shader ignores runtime color changes.

---

## Development Setup

### Requirements
- Unity 2022.3 LTS with Android Build Support + Android SDK/NDK
- Meta XR Core SDK (Unity Asset Store)
- Meta XR Interaction SDK (Unity Asset Store)
- Android API Level 34, IL2CPP, ARM64

### Running in Editor
1. Clone repo
2. Open in Unity Hub → Add project from disk
3. Open `Assets/Scenes/ArenaScene.unity`
4. Hit Play — click Game window to focus, use keyboard shortcuts to cast spells

### Deploying to Meta Quest
1. Enable Developer Mode on Quest (Settings → Developer Mode)
2. Connect via USB-C
3. File → Build Settings → Android → Build and Run

---

## Demo Script (5 Minutes)

1. **(0:30)** Hand headset to judge. Let them look around the arena.
2. **(1:00)** Wave spawns. Point out formation — Boss at back, archers flanking, goblins in front.
3. **(1:00)** Point at the runtime line overlay. *"Every box is an Octree node. Watch them subdivide as enemies cluster — O(log n + k) AoE queries instead of O(n) brute force."*
4. **(1:00)** Show the floating Trie panel. *"Each hand sign traverses this prefix tree. O(L) spell recognition regardless of how many spells exist. Follow the gold path and read the terminal spell names."*
5. **(0:30)** Point at white ring enemy. *"Fibonacci Heap maximum — highest threat, O(1) to find. Watch the ring change when the Boss dies."*
6. **(0:30)** Show the floating cooldown strip. *"Skip List sorted by expiry time. O(1) peek at which spell refreshes next."*
7. **(0:30)** Open Q&A.

---

## References

1. Sartaj Sahni, Dinesh P. Mehta — *Handbook of Data Structures and Applications*, 2nd Ed.
2. T. Cormen et al. — *Introduction to Algorithms*, 2nd Ed., PHI
3. Peter Brass — *Advanced Data Structures*, Cambridge University Press
4. Meta XR SDK Documentation — developer.oculus.com
5. Unity XR Interaction Toolkit — docs.unity3d.com

---

*Built by Team Threshold — Tesseract '26, Open Innovation Track, SIG Reality Spectra, VIT Pune*
