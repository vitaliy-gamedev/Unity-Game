# DEADBAND Tactical Squad Extraction

A student Unity prototype presented for the Unity Development course at IT STEP Academy. The gameplay demonstration shows squad combat, weapon handling, menu settings and a SIGNAL-driven extraction mission. This is a learning project, not a finished commercial game.

**[Watch the gameplay demonstration](https://www.linkedin.com/feed/update/urn:li:activity:7501318394416328704/)**

## What is included

This portfolio snapshot contains 57 gameplay C# source files from the local DEADBAND project. Source is grouped by responsibility under `Source/`: AI, audio, camera, combat, inventory, missions, progression, SIGNAL and squad control.

No Unity scenes, prefabs, models, textures, animation clips, music, build files or third-party packages are redistributed here. The commercial GameFoundation toolkit and editor builders are also excluded.

## Contribution and AI assistance

I assembled and configured the Unity project, integrated assets, scenes and menus, tested gameplay scenarios, reported issues and prepared the student demonstration. I wrote some small scripts and directed implementation changes.

ChatGPT / Codex substantially assisted with code implementation, debugging and corrections. The code is presented transparently as AI-assisted project work. Third-party assets used in the demonstration remain the property of their respective creators.

## Source map

| Area | Examples | Purpose |
| --- | --- | --- |
| SIGNAL | `SignalSystem`, `SignalConfig`, `SignalReceiver` | Distance-based strength and event-driven reactions |
| Squad | `SquadFormation`, `SquadCommandController`, `CompanionSquadAI` | Formation slots, orders and companion behaviour |
| Combat | `PlayerWeaponController`, `Health`, `CombatNoiseSystem` | Weapon handling, damage and combat noise |
| Missions | `MissionManager`, `MissionOutcomeRules`, `ExtractionPoint` | Objective flow and full, partial or failed extraction |
| Progression | `CampaignProfile`, `CampaignProgression` | Local progression data |
| Audio | `AdaptiveMusicDirector`, `WeaponAudioEmitter` | Gameplay audio reactions |

## Suggested code review order

1. `Source/Signal/SignalConfig.cs` and `SignalSystem.cs`: configuration, sampling and events.
2. `Source/Squad/SquadFormation.cs`: slot generation and nearest-slot assignment.
3. `Source/Mission/MissionOutcomeRules.cs`: extraction result rules.
4. `Source/Mission/MissionManager.cs`: orchestration and dependency on GameFoundation.
5. `Source/Combat/PlayerWeaponController.cs`: player-facing combat flow.

## Runtime and dependencies

The local project records Unity **6000.3.6f1**, Universal Render Pipeline **17.3.0**, Input System **1.18.0**, AI Navigation **2.0.14** and Cinemachine **3.1.7**.

This snapshot is for code review only. It is **not independently runnable**: scenes, serialized assets and presentation resources are omitted. `MissionManager` references the separately distributed `GameFoundation.Core` integration. No clean-import or compile pass is claimed for this source-only export. The gameplay video is evidence of the student demonstration, not a guarantee that every local revision is fully tested.

## Rights and exclusions

No open-source license is granted by this showcase. Review the source as portfolio evidence; contact the author before reuse or redistribution. This repository does not grant any rights to third-party resources visible in the gameplay video.

## Links

- [Portfolio](https://gamefoundation-pro.nortczov.chatgpt.site/)
- [LinkedIn](https://www.linkedin.com/in/vitaliy-nortczov-39ab00400)
- [GitHub profile](https://github.com/vitaliy-gamedev)
