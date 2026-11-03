# Changelog

The addon SDK is numbered separately from the gamemode: `GwrpSdk.Version`
goes up when a surface changes in a way an addon built against an older one
could notice. An addon says which SDK it was built against
(`SdkVersion => GwrpSdk.Version`), and a server running another prints a
warning but still loads it.

| SDK | Gamemode | What changed |
|---|---|---|
| 1 | v1.5.0 | First release: items with behaviour, parts on every player, chat commands, admin commands and permissions, server settings, HUD panels, help tabs, world entities, usable objects. |
| 2 | v1.5.13 | Events for what happens to players and across the game (`IPawnEvents`, `IServerEvents`, payday), Use prompts on players (`IPawnInteractable`), movement orders (`WalkTo`, `Follow`, `Hold`) and pushes, poses and animation values, your own animation of the body every frame (`IPawnAnimator`: hand IK, bones, graph values), badges on nameplates and the admin panel (`IPawnBadges`), saved data per player and per character (`AddonPlayerData`), credits (`Economy`), flags on jobs and regiments, chat filters (`IChatFilter`), player-menu tabs, NPC and vehicle events and addon vehicle drives, the map's doors, teleporters, buttons and text screens (`MapObjects`), rules for damage, respawning, jobs, ranks and trainings, names and shops (`IDamageRules`, `IRespawnRules`, `IJobRules`, `IRankRules`, `INameRules`, `IShopRules`), your own power, on-hit and weapon behaviours, NPC routines, chat channels, radio networks, voice rules, camera and HUD hooks, players-list columns and admin panel views. One rename: `DamageInfo` is now `DamageSource` (it clashed with s&box's own `DamageInfo`). |
| 3 | v1.6.14 | Entity options: a kind's own settings panel in the spawn menu before it's placed (`HasOptions`, `CreateOptions`, `EntitySpawn.Options`), and for a placed one, opened with Use and the toolgun (`ReadOptions`, `ApplyOptions`, `Actions`, `RunAction`, `Summary`). |

## Template

- 2026-10-28: first version, with the Flares example and the glide pack.
