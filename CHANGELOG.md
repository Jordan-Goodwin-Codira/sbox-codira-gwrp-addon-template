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
| 4 | v1.6.18 | Restraints in your own words: `PawnEffects.ApplyRestraint( seconds, notice )` tells the player `notice` (empty for nothing) instead of "You are restrained for N s". The kneeling pose (`PawnPose.Kneel`) now kneels whatever s&box's controller writes, and lowers the player's camera. Melee weapons do only what their file says: right click blocks only with Alt-Fire Mode `Block`, swings turn bolts back only with `DeflectsBolts`. A weapon behaviour can draw its own hits (`WeaponBehaviour.DrawsOwnImpacts`): the gamemode then leaves out its hit sparks. |
| 5 | v1.6.25 | A body of your own in a player's or NPC's place: a part with `IBodyStandIn` that `HidesBody` hides their body and what's in hand on every machine, lets others pass through them and hides their nameplate; its `BodyStandIn` (a ragdoll) is where their death ragdoll starts if they die meanwhile. Parts on NPCs: `INpcAddon` components go on every NPC as it spawns (`this.GetNpc()`). Restrained NPCs stand still and don't move, aim or fire. A weapon behaviour can hold a shot with a reason (`WeaponBehaviour.CanFire`): nothing fires, no ammo is spent, the player is told why. Gear: a part that's `IPawnGear` is something the player wears, shown on the HUD under the radio and worked with the new Gear key (N); with more than one piece, a wheel to choose (`PawnGears.Equipped`). |

## Template

- 2026-10-28: first version, with the Flares example and the glide pack.
