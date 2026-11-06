# Making addons for Galaxy Wars RP

An addon is a package with C# in it. Server owners mount it like any other
package; its code runs on the server and on every player's game, and the
gamemode finds what it adds by the base classes it derives from: an item
type, a part on every player, a chat command, an admin command, a settings
section, a HUD panel, a help tab, a kind of thing admins place in the world.
Nothing in the gamemode changes, and you never need its source.

This repository is the template: a working addon with one example of each
surface, a [README](README.md) that walks through making it yours, and a
script that publishes it. What follows is the reference for each
surface, then the hooks into the rest of the game: what happens to players,
moving, posing and animating them, badges, saved data, credits, chat, the
map, NPCs and vehicles, and the rules, behaviours, channels, voice, camera
and HUD hooks that let an addon change what the gamemode decides. Last, the
sounds, fire, smoke and explosions you can use without shipping your own.

## Setup

An addon project is an ordinary package project: a `.sbproj` (the
template's has everything set) with a `Code/` folder beside `Assets/`. Its
`ParentPackage` is what makes it work: the editor compiles your code against
the **published** gamemode, so every public type in it (`ItemResource`,
`Pawn`, `GwrpAddon`, `EntityService`…) is there to derive from and call.
Put `global using SWRP;` in `Code/Assembly.cs` with the usual ones, and keep
your own code in a namespace of your own: the gamemode's is `SWRP`, and
anything in it is taken for the gamemode's.

Your code is compiled by every machine that mounts the package, under
s&box's rules for game code (the same whitelist of types and methods the
editor applies to the gamemode), so if it compiles in your editor it runs
everywhere.

At the moment s&box's normal branch can't open an addon project: the editor
stops at "Loading parent package"
([sbox-public #12225](https://github.com/Facepunch/sbox-public/issues/12225)).
Open it on the **staging** branch until that's fixed (Steam: right-click
s&box, Properties, Betas). Players and servers don't need it.

## The manifest

One class deriving from `GwrpAddon` per package:

```csharp
public sealed class FlaresAddon : GwrpAddon
{
	public override string Id => "yourorg.flares";       // the package's ident
	public override string Name => "Flares";
	public override string Version => "1.0";
	public override int SdkVersion => GwrpSdk.Version;   // baked in when you compile
	public override IEnumerable<PermissionDef> Permissions => new[]
	{
		new PermissionDef { Key = "addon.flares.light", Description = "Light a flare on any player" },
	};
	public override void OnLoaded() { }                   // once per machine, when the code is in
}
```

The manifest's namespace is what marks the rest of your types as the
addon's (`ExtraTypes` lists any outside it). The Mounts tab names the
addon under its package's row, with the SDK version it was built for; the
help screen lists it; and when the server owner removes the mount, your
types are skipped until the server restarts (code can't be unloaded). A
package with code but no manifest still works, with one warning in the log,
but can't be named or turned off cleanly.

`SdkVersion` is only a warning: the surfaces grow without breaking, so an
addon built for SDK 1 keeps working on SDK 2 unless the changelog says
otherwise.

## How it loads

When a package is mounted, on the server at start-up or live from the Mounts
tab, the engine puts its compiled code into the type library first and its
files (your `.flare` items, models, sounds) after, on the host and on every
player's game, including players who join later. The gamemode then:

1. Greets new manifests: logs `[addon] Flares 1.0 (yourorg.flares) loaded:
   SDK 1`, warns about an SDK mismatch, runs `OnLoaded`.
2. Puts a part of each `IPawnAddon` type on players as they spawn (players
   already in the game get theirs when they next respawn).
3. Lists your commands, settings, panels, tabs and entity kinds wherever
   the gamemode's own are listed, from then on, on each read.

There is no registration step. Every list is built on each read from what's
loaded, which is also why nothing of yours should cache types or instances
in a static field: a hotload in the editor replaces the types, and the next
read picks the new ones up.

Names that clash are dropped, with one warning in the log: a chat command
named like one of the gamemode's, an entity kind id or help tab id another
addon already uses, a permission key not starting with `addon.`. The
gamemode's own always win; between addons, the first by type name wins. So
prefix every id with your slug.

Every hook you implement is called through a guard: an exception is logged
once per hook, with its stack, and the game carries on without it; where a
player was waiting on it they see "FlareItem failed; the server log has
more." Hooks run on the machine their summary says, and many run every
frame or on every read, so keep them cheap.

## Items with behaviour

Derive from `ItemResource` and give the type a file extension of its own:

```csharp
[AssetType( Name = "Flare", Extension = "flare", Category = "Flares" ), Title( "Flare" )]
public sealed class FlareItem : ItemResource
{
	/// <summary>How long the flare burns.</summary>
	[Property, Title( "Burn seconds" ), Group( "Flare" ), Range( 5f, 300f )]
	public float BurnSeconds { get; set; } = 60f;

	public override bool HasUseAction => true;
	public override string KindLabel => "Flare";
	public override string UseSummary => $"Lights a flare for {BurnSeconds:0} s";
	public override string WhatItDoes() => "Drops a burning flare where you aim.";

	public override bool Use( ItemUse use, out string error )   // host
	{
		var hit = use.Trace( 400f );
		...
		error = ""; return true;
	}
}
```

The item has every field an item has (model and its scale, weight, stack,
**Everyone carries one**, the weapon it hands out, cooldown, range, whether
a use consumes one) plus yours, and goes everywhere an item goes: kits,
vendors, inventories, the spawn menu, `ItemResource.Find( id )`. Keep
the extension short (five letters or fewer is safe): a long one can
silently never compile. Its files live in `Assets/<slug>/gameresources/items/`, made with
right-click → Create in the editor's asset browser (restart the editor once
after adding the type, so it notices files with the new extension).

What it does and says are virtual members; the base ones are the gamemode's
own kinds, picked by the **Kind** field, which a derived type leaves at
`None` and the Server Owner panel hides for it:

| Member | Runs on | What it is |
|---|---|---|
| `HasUseAction`, `UseLabel`, `CanUseFromMenu` | every machine | Whether it's used at all (left click in hand, or the inventory's Use button), what the button says, and whether Use works from the inventory, where there's nothing to aim at |
| `KindLabel`, `WhatItDoes()`, `UseSummary` | every machine | The line under its name in the inventory, the sentence in its details, the few words in the spawn menu |
| `CanUse( user, out why )` | host | Before the cooldown and before anything is taken |
| `Use( ItemUse, out error )` | host | The use. True starts the cooldown and, if `ConsumesOnUse( use )` says so, takes one from the stack; false shows `error` |
| `CooldownSeconds`, `DropsOnDeathByDefault` | host | Seconds between uses; whether it falls when its owner dies (before any job's exception) |
| `OnEquipped`, `OnHolstered`, `OnHeldUpdate( holder )` | every machine | Taken in hand, put away (or dropped, or died with it in hand), every frame in hand |
| `OnAdded`, `OnRemoved( owner, count )`, `OnOwnerDied( owner, dropped )` | host | Inventory changes, and death with it |
| `ShowsField( field, value )` | Server Owner panel | Which of its fields show, given the others' values |

`ItemUse` carries the user, the item (with the server owner's edits on it),
where they aimed from and which way, and the lookups a use usually needs:
`ResolveTarget` (by the item's **Target** field and `Alt`: right click, or the inventory's button, as against left click), `ResolveAimedPawn( out
error, range )`, `Trace( range )` and `Notify( message )`.

Server owners can make items of your type in game (**+ New** on the Items
tab offers each mounted addon's types) and edit yours field by field, as
with any item; what they make is saved with your type's name and waits,
with its fields, while your addon isn't mounted. A `///` summary on a field
is its description on the panel, `[Title]` its label and `[Group]` its
section.

## Parts on every player

A component marked `IPawnAddon` is created on every player's object before
the object is spawned over the network, beside the gamemode's own parts, so
its `[Sync]` state reaches everyone, late joiners included. Derive from
`Component` directly: the engine's `[Sync]` code generation can't see
through a base class from another package.

```csharp
public sealed class GlidePack : Component, IPawnAddon, IActionBlocker, IMovementModifier
{
	[Sync] public bool Gliding { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( !this.IsLocalPawn() ) return;      // the player's own machine
		var pawn = this.GetPawn();
		Gliding = pawn.Controller.IsAirborne && Input.Down( "Jump" );
	}

	public bool Blocks( PawnAction action, out string reason )
	{
		reason = "Not while you're gliding.";
		return Gliding && action == PawnAction.Attack;
	}

	public float SpeedMultiplier => Gliding ? 0.5f : 1f;
}
```

`IActionBlocker` is asked before each thing a player might do:
`PawnAction.Move`, `Look`, `Attack`, `UseItems`, `UseTools`, `Seat`, `Pay`,
`Radio`, `SwitchCharacter` and `BeTargeted` (by homing bolts and NPCs); the
gamemode's own blocks (dead, frozen, restrained, customising) come first.
`IMovementModifier` multiplies the walk and run speed; every enabled one
combines. Both are asked on whichever machine asks, so read synced state.
Your own code asks the same question with `pawn.Can( PawnAction.Attack, out
var why )`. A part that needs a move mode of its own declares it with
`[RequireComponent]`. Players already in the game when an addon is mounted
live get its part when they next respawn.

## Hearing what happens

A player part can implement `IPawnEvents` to hear what happens to the
player it's on, and any other class (your manifest is a good home) can
implement `IServerEvents` to hear what happens across the game. Every
method is optional: write only the ones you need. They all run on the host.

```csharp
public sealed class BountyAddon : GwrpAddon, IServerEvents
{
	...
	public void OnPawnDied( Pawn pawn, DamageSource lastHit )
	{
		if ( lastHit.HasAttacker && Players.Find( lastHit.AttackerSteamId ) is { } killer )
			Economy.Give( killer, 50, $"bounty on {pawn.DisplayName}", this );
	}

	public int AdjustPay( Pawn pawn, int amount ) => pawn.HasFlag( "addon.bounty.hunter" ) ? amount * 2 : amount;
}
```

| `IPawnEvents` (on a part) | `IServerEvents` (anywhere) | When |
|---|---|---|
| `OnSpawned()` | `OnPawnSpawned( pawn )` | The player is in the world: joined, picked a character, or back after a map change |
| `OnRespawned()` | `OnPawnRespawned( pawn )` | Back at a spawn point with full health: after going down, or a job change |
| `OnDamaged( hit, amount )` | | Hurt; `amount` is what got through their armour |
| `OnDied( lastHit )` | `OnPawnDied( pawn, lastHit )` | Went down |
| `OnJobChanged( old, new )` | `OnJobChanged( pawn, old, new )` | A new job (either side null for none) |
| `OnRegimentChanged( old, new )` | `OnRegimentChanged( pawn, old, new )` | Joined, left or moved regiment (ids) |
| `OnRankChanged( old, new )` | `OnRankChanged( pawn, old, new )` | Promoted, demoted or moved (rank ids) |
| `OnCreditsChanged( old, new )` | `OnCreditsChanged( pawn, old, new )` | Their credits changed, by any means |
| `OnCharacterChanged()` | `OnCharacterChanged( pawn )` | Switched to another of their characters |
| `OnSeatEntered( vehicle, seat )` | `OnSeatEntered( pawn, vehicle, seat )` | Got in a vehicle |
| `OnSeatLeft( vehicle )` | `OnSeatLeft( pawn, vehicle )` | Got out, or was put out |
| | `OnPlayerJoined( connection )` | Connected, before picking a character |
| | `OnPlayerLeft( connection, pawn )` | Leaving (`pawn` is null if they never picked a character); runs before they're saved |
| | `AdjustPay( pawn, amount )` | Payday: return what this player gets (their salary, or what the addon before yours returned) |
| | `OnPaid( pawn, amount )` | Payday: what they got |
| | `OnNpcSpawned( npc )`, `OnNpcDied( npc, lastHit )` | NPCs |
| | `OnVehicleSpawned( vehicle )`, `OnVehicleDestroyed( vehicle, lastHit )` | Vehicles |

Job, regiment, rank and credit changes are noticed within a tick, whatever
caused them. `DamageSource` is who hit, with what and where:
`HasAttacker`, `AttackerId` (their object), `AttackerSteamId` (0 for an
NPC), `WeaponId`; a fall, burning or an admin's slay has no attacker. A
fresh `IServerEvents` object is made for each event, so keep nothing on it.

## Badges on players

```csharp
public sealed class WantedPart : Component, IPawnAddon, IPawnBadges
{
	[Sync( SyncFlags.FromHost )] public bool Wanted { get; set; }

	public IEnumerable<PawnBadge> Badges( Pawn viewer )
	{
		if ( Wanted ) yield return new PawnBadge( "Wanted", Color.Red );
	}
}
```

Badges are small tags under the player's name on their nameplate, beside
Frozen, Muted and the rest in the admin panel's player list, and on their
row in the Tab players list (`BadgePlace.Nameplate`, `AdminPanel`,
`PlayerList`, or `Everywhere`, the default). Every machine that draws
the player asks every frame, so read synced state. `viewer` is the local
player, so a badge can show only to some (a regiment's own medics, say). At
most four show; a colour too dark for the plates is lifted.

## Gear the player wears

Something worn and worked with a key (a flashlight on the chest, goggles, a
jetpack) is gear: mark a player part `IPawnGear` and the gamemode's Gear key
(N unless the player changed it on s&box's Key binds page) works it.

```csharp
public sealed class Goggles : Component, IPawnAddon, IPawnGear
{
	[Sync( SyncFlags.FromHost )] public bool On { get; set; }

	public string GearId => "goggles.nightvision";
	public string GearName => "Night vision";
	public bool IsEquipped => this.GetPawn()?.Inventory.CountOf( "goggles" ) > 0;
	public string GearStatus => On ? "On" : "Off";
	public bool GearActive => On;
	public string GearAction => On ? "switch them off" : "switch them on";
	public string GearIcon => "visibility";

	public void UseGear() => On = !On;   // host
}
```

While a piece is equipped, the HUD shows it top left, under the radio: the
key, its name and `GearStatus`, lit while `GearActive`, and a line saying
what the key does ("Press N to switch them on"). A press of the key asks the
host, which calls `UseGear` if the piece is still equipped and the player may
use things (`PawnAction.UseItems`: not restrained, frozen or dead), so change
synced state there. With more than one piece equipped, the HUD lists them
all, a tap works the chosen one, and holding the key opens a wheel to choose
(`GearIcon` is its Material Icons name there). `IsEquipped` and the HUD's
members are asked often on every machine: read synced state, keep them
cheap. `PawnGears.Equipped( pawn )` lists a player's equipped gear, any
addon's.

## Using other players

```csharp
public sealed class SearchPart : Component, IPawnAddon, IPawnInteractable
{
	public string UsePrompt( Pawn looker ) =>
		this.GetPawn().Effects.IsRestrained ? $"Search {this.GetPawn().DisplayName}" : null;

	public bool OnUsed( Pawn user, out string error )   // host
	{
		error = "";
		...
		return true;
	}
}
```

When someone looks at the player within `UseReach` (110 units unless you
say), the first part with a prompt for them shows it as the usual "[E]"
prompt; the gamemode's own "Drag" on a restrained player comes first. On
Use the host checks the reach and asks `UsePrompt` again before calling
`OnUsed`, so a modified game can't skip either; false with an `error` tells
the user why.

## Moving players

The host can move a player for you; their own game steers the body, as it
does for a drag, so it works on dedicated servers:

```csharp
pawn.Movement.WalkTo( cell.WorldPosition, done: arrived => { ... } );
pawn.Movement.Follow( officer, distance: 60f );
pawn.Movement.Hold( 5f );
pawn.Movement.ClearOrder();
pawn.Push( Vector3.Up * 400f );
pawn.Teleport( position, rotation );
```

While an order holds (`pawn.Movement.HasOrder`), the player's own keys don't
move them and `pawn.Can( PawnAction.Move )` is false. `done` hears true when
a walk arrives or a timed follow or hold runs out, and false when the walk
gets stuck or times out, the order is cleared or replaced, or the player
goes down, gets in a seat or is teleported. `speed` (units a second; 0 for
their walking pace) and `seconds` (0 for until cleared) are optional on each.

## Poses and animation

```csharp
pawn.Appearance.SetPose( PawnPose.FloorKneesUp, locked: true, hideHeldItem: true );
pawn.Appearance.ClearPose();
pawn.Appearance.SetHeldItemHidden( true );
pawn.Appearance.SetAnimParameter( "b_swim", true );
pawn.Appearance.ClearAnimParameters();
```

`PawnPose` is `Sit`, `SitLeaningForward`, `SitRelaxed`, `Floor`, `FloorTwo`,
`FloorKneesUp`, `FloorLeaningBack` (the animation's sitting poses, as
vehicle seats use them) or `Kneel`. A posed player doesn't walk but can
look about; an unlocked pose ends when they move or jump. `hideHeldItem`
puts away what's in hand meanwhile: not shown, not held, can't attack.
`SetAnimParameter` sets any value of the body's animation graph (up to 32
per player), applied after the gamemode's own every frame on every machine,
until you clear it. All of these are host calls and synced.

To hold a player still for a while (a stun), `pawn.Effects.ApplyRestraint( seconds, notice )` on the host: they can't move, fight or use items, but can look about, and are told `notice` (empty for nothing). A longer hold already on them (cuffs, an admin) is yours to leave be: check `Effects.IsRestrained` and `Effects.RestrainedUntil` first.

### Your own animation

For anything else, animate the body yourself: a player part implementing
`IPawnAnimator` is called on every machine, every frame, after the
gamemode's animation, with the body's renderer, its bones and hand IK.
Nothing in the gamemode needs to change for a new pose.

```csharp
public sealed class HandsUpPart : Component, IPawnAddon, IPawnAnimator
{
	[Sync( SyncFlags.FromHost )] public bool HandsUp { get; set; }

	public bool HoldsHands => HandsUp;

	public void Animate( PawnAnimation animation )
	{
		if ( !HandsUp || animation.Bone( "head" ) is not { } head ) return;
		var up = Rotation.LookAt( Vector3.Up, Vector3.Backward );
		animation.SetHandTargets(
			new Transform( head.Position + new Vector3( 2f, 10f, 12f ), up ),
			new Transform( head.Position + new Vector3( 2f, -10f, 12f ), up ) );
	}
}
```

While a part says it `HoldsHands`, what's in hand is put away (not shown,
not held, the arms hang as if empty) and the weapon's hold and the typing
pose let go, so `SetHandTargets( left, right )` places the wrists, in the
body's space (X forward, Y left, Z up), and the graph's arm IK bends the
elbows to reach them. `Bone( name )` and `BonePosition( name )` read the
body's bones as last animated (`pelvis`, `spine_2`, `head`, `arm_upper_L`,
`hand_R` and the rest), so a pose can fit itself to any playermodel;
`Set( parameter, value )` sets graph values for the frame, and `Renderer`
and `Helper` are the engine's own objects for anything more. Ease in and
out yourself (`PawnAnimation.Blend` helps), and drive it all from synced
state, so every machine draws the same. `HoldsHands` doesn't stop attacks;
pair it with an `IActionBlocker`. The handcuffs addon holds cuffed players'
hands behind their back this way.

To draw a body of your own in a player's place (a ragdoll when they're
knocked down), add `IBodyStandIn` to the part: say so with `HidesBody` and
name the body with `BodyStandIn`. It works the same on an NPC part (see
NPCs and vehicles).

```csharp
public sealed class KnockdownPart : Component, IPawnAddon, IBodyStandIn
{
	[Sync( SyncFlags.FromHost )] public GameObject Ragdoll { get; set; }   // from Ragdolls.Spawn, on the host

	public bool HidesBody => Ragdoll.IsValid();
	public SkinnedModelRenderer BodyStandIn => Ragdoll.IsValid() ? Ragdoll.Components.Get<SkinnedModelRenderer>() : null;
}
```

While a part `HidesBody`, the body and what's in hand aren't drawn on any
machine, a player's hands are held, nobody bumps into them (their capsule
passes through players and NPCs, as a downed player's does), and nobody sees
a nameplate over them. Their capsule stays where it is, so move it to your
body when you're done (`pawn.Teleport` or `npc.Teleport`, on the host)
before you stop hiding it. If they die while it's hidden, their death ragdoll
starts from `BodyStandIn`, where it lies and in its pose; take yours away
then (`IPawnEvents.OnDied` on a player, `npc.IsDead` on an NPC).
`Ragdolls.Spawn( gameObject, name )` makes a networked ragdoll in the body's
pose (host only). The GWRP - Taser addon knocks players and NPCs down this
way.

## Saving data about players

```csharp
public sealed class BountyRecord
{
	public int Collected { get; set; }
}

var record = AddonPlayerData.Update<BountyRecord>( pawn.SteamId, r => r.Collected++ );
var mine = AddonPlayerData.GetForCharacter<BountyRecord>( pawn );
```

Your own class with public properties, saved as JSON on the server in
`server/addons/<your addon's id>/players/<steam id>.json` (never on players'
machines). `Get`, `Set`, `Update` and `Clear` keep one per player, shared by
all their characters, and work for players who are offline;
`GetForCharacter` and the rest keep one per character, for the one they're
playing. `Get` hands back a copy: change it with `Update` or `Set`. Writes
reach the disk within a minute and when the player leaves; each player's
file holds up to 256 KB. Host only: copy what players need to see onto a
`[Sync]` property of your part.

## Credits

```csharp
var credits = Economy.Credits( pawn );                       // any machine
Economy.Give( pawn, 100, "found a cache", this );            // host
if ( !Economy.Take( pawn, 250, "bought a licence", out var error, this ) ) ...
Economy.Transfer( from, to, 50, "paid a fine", out error, this );
Economy.Set( pawn, 0, "reset by event", this );
```

Each change writes an `[economy]` line to the server log with your addon's
name (pass `this`) and the reason. `Take` and `Transfer` only go ahead if
the payer has the full amount.

## Flags on jobs and regiments

Declare switches in your manifest; the server owner ticks them on jobs and
regiments in the Server Owner panel (an **Addons** section appears once an
addon declares one), and you check them anywhere:

```csharp
public override IEnumerable<AddonFlag> Flags => new[]
{
	new AddonFlag( "addon.bounty.hunter", "Bounty hunter", "Collects bounties and is paid double." ),
	new AddonFlag( "addon.bounty.exempt", "No bounty", "", AddonFlagFor.Regiments ),
};

if ( pawn.HasFlag( "addon.bounty.hunter" ) ) ...
```

A player has a flag when their job or their regiment has it ticked. Keys
start with `addon.` and your slug; ticks for an addon that isn't mounted
are kept.

## Chat commands

```csharp
public sealed class FlaresCommand : ChatCommand
{
	public override ChatCommandInfo Info =>
		new( "Flares", new[] { "flares" }, "[player]", "Say how many flares are burning." );

	public override void Run( Pawn sender, string args )   // host
	{
		if ( !TryTakeTarget( sender, args, "flares", out var target, out var rest ) ) return;
		NotifyInfo( sender, $"..." );
	}
}
```

`ChatCommandInfo` is the group it's listed under, its names (the first is
the main one), what goes after it, what it does, and who may use it:
`CommandUsers.Everyone`, `Officers`, `NpcCommanders` or `Permission` with a
key. The gamemode checks that before `Run`, lists the command in the help
screen's Commands tab and the chat's list as you type, and drops a name one
of its own commands has. `Talks` says the command puts the sender's words in
chat, so a mute blocks it. The helpers are `NotifyError`, `NotifyInfo`,
`NotifySuccess`, `FindPawnByName`, `TryTakeArg` (quoted names), `TryTakeTarget`,
`SystemMessage( to, tag, text )` and `Announce( tag, text )`.

## Chat filters

```csharp
public sealed class CensorFilter : IChatFilter
{
	public void Filter( ChatContext message )   // host
	{
		if ( message.Channel == ChatChannel.OOC && message.Text.Contains( "http" ) )
			message.Block( "Links aren't allowed in OOC." );
		message.Text = message.Text.Replace( "darn", "****" );
	}
}
```

Every message a player sends (speech, `/me`, `/ooc`, radio, whispers and the
rest; not system messages) passes through each filter on the host after the
gamemode's own mute, in order of the filters' full type names. Change
`Text`, add to or take from `Recipients` (Steam ids; null means everyone),
or `Block( reason )`, which tells the sender the reason if there is one and
skips the filters after yours. `Sender`, `Channel`, `IsAction`, `Frequency`
(the radio channel) and `TargetSteamId` (a whisper's) say what it is.

## Admin commands and permissions

```csharp
public sealed class LightFlareOnCommand : AdminPlayerCommand
{
	public override AdminCommand Info => new()
	{
		Id = "flares.light", Name = "Light a flare", Glyph = "🔥",
		Permission = "addon.flares.light", Description = "Light a flare at the player's feet.",
		Params = new[] { new CommandParam { Key = "seconds", Label = "Burn seconds", Type = "number" } },
	};

	public override bool Run( AdminCommandRun run )   // host
	{
		if ( run.Target.IsDead ) return run.Fail( "They're down." );
		run.TellTarget( $"{run.Actor.DisplayName} lit a flare at your feet." );
		run.AuditDetails = run.Arg( "seconds" );
		return true;
	}
}
```

The command is a button on the admin panel's Players tab (under a chip
named after your addon unless `Category` says otherwise), with its options
filled in first: `Type` is `text`, `number`, `duration`, `job`, `rank` or
`training`. The gamemode checks the permission and that the admin outranks
the target, runs it, and writes the audit log; `ButtonFace( target )` can
change what the button says for a toggle. `run` has `Actor`, `Target`,
`Arg`/`IntArg`/`FloatArg`, `Fail( why )`, `Tell`, `TellTarget` and
`Announce`.

Permissions your addon checks start with `addon.` (`addon.flares.light`),
are declared in the manifest, and appear in the role editor under your
addon's name while it's mounted (a key a command names without declaring is
listed too). The gamemode's own keys (`Permissions.AdminSpawn` and the rest)
can be checked but not redeclared. Check one yourself with
`PermissionService.HasPermission( pawn, key )`.

## Server settings

```csharp
public sealed class FlareSettings : AddonSettings
{
	public override string Id => "flares";          // the file's name
	public override string Title => "Flares";
	public override string Description => "How flares placed by admins behave.";

	[Property, Title( "Burn seconds" ), Range( 5f, 300f )] public float BurnSeconds { get; set; } = 60f;
	[Property, Title( "Show the flare count" )] public bool ShowCount { get; set; } = true;

	public override void OnChanged() { }            // every machine
}
```

Each class is a section on the Server Owner panel's Settings tab, edited
like any content (**Apply** and **Discard**, `server.manage`). The host
keeps the values in `server/addons/<id>.json` in its data folder, clamps
numbers to their `[Range]`, and sends them to every player, joiners
included; read them anywhere with `AddonSettingsStore.Get<FlareSettings>()`,
which is the same instance until the values or the code change. The fields
can be anything the Server Owner panel edits: text, numbers, on/off,
enums, colours and lists of text.

## HUD panels and menu tabs

A HUD panel is a Razor panel deriving from `AddonHudPanel`:

```razor
@using Sandbox.UI
@inherits SWRP.UI.AddonHudPanel
@namespace YourOrg.Flares

<root class="anchor-top-right hud-plate">
	<div class="hud-title">Flares</div>
	<div class="hud-value">@Burning burning</div>
</root>

@code
{
	int Burning => EntityService.All( Game.ActiveScene ).Count( e => e.KindId == "flares.flare" );
	public override bool IsVisible( Pawn local ) => Burning > 0;   // read every frame
	protected override int BuildHash() => Burning;
}
```

It goes on the addon layer of the HUD, which hides with the rest of the HUD
(`/hud`), scales with it and steps aside while a menu is up; `Order` sorts
panels. Your own `.razor.scss` styles your own classes, but can't reach the
gamemode's variables, so the layer's stylesheet gives you its look through
utility classes: `anchor-top-left`, `anchor-top-centre`, `anchor-top-right`,
`anchor-centre-left`, `anchor-centre`, `anchor-centre-right`,
`anchor-bottom-left`, `anchor-bottom-centre`, `anchor-bottom-right` (where
it sits, clear of the gamemode's own corners); `hud-plate` (a dark card);
`hud-title`, `hud-label`, `hud-value`, `hud-muted` (text); `hud-row`,
`hud-stack`, `hud-kv` (layout); `hud-bar` with a `.fill` inside whose width
you set; `hud-pill`, `hud-keycap`; the colours `good`, `warn`, `bad`,
`accent`; and `clickable` for a panel that takes the mouse.

A menu tab is a class deriving from `AddonMenuTab`: `Id` (`flares.help`),
`Title`, `Order`, `IsVisible( local )` and `Create()`, which returns a fresh
panel for the tab's body each time it opens. It goes in the help screen (H)
after Settings, or, with `Host => AddonMenuHost.Player`, in the player
menu's sidebar after Players, with `Icon` (a Material icon name, `"science"`)
beside its title. The help screen's classes `section-title` and `hint` work
in a help tab. `AddonMenuTabs.Open( id )` opens the menu at your tab, from a
key of yours or a HUD button.

## Things in the world

A kind of thing admins can place:

```csharp
public sealed class FlareKind : EntityKind
{
	public override string Id => "flares.flare";
	public override string Label => "Flare";
	public override string PreviewModel => "models/dev/sphere.vmdl";   // the menu's thumbnail and the toolgun's ghost

	public override GameObject Spawn( EntitySpawn spawn, out string error )   // host
	{
		error = "";
		var go = new GameObject( true, "Flare" );
		go.WorldPosition = spawn.Position;
		go.Components.Create<ModelRenderer>().Model = Model.Load( PreviewModel );
		go.Components.Create<Flare>();
		return go;                                  // the gamemode spawns it over the network
	}

	public override JsonObject Save( GameObject go ) => new() { ["seconds"] = go.Components.Get<Flare>().SecondsLeft };
	public override void Load( GameObject go, JsonObject data ) => go.Components.Get<Flare>().SecondsLeft = (int)data["seconds"];
}
```

It appears in the spawn menu's **Entities** chip and the toolgun's
**Entities** mode for admins with its `Permission` (`admin.entity` unless
you name one of your own); the gamemode spawns it over the network, puts it
on the admin's Z undo, lets the physgun carry it (`Movable`), and keeps it
in `/savemap` (`Persists`) with whatever `Save` returns, giving it back to
`Load` when the map loads. An entity whose addon isn't mounted when the map
loads stays in the save until it is. `Place( user, trace )` decides where
an aimed placement goes (on the hit point, facing the admin, unless you
snap or lift it), `Describe( go )` is the physgun's label and the toolgun's
remove prompt, and `OnRemoved( go )` hears about the toolgun, undo and map
changes. Every spawned one carries a `SpawnedEntity` component (`KindId`,
`IsMapContent`, `SpawnerSteamId`), and `EntityService.All( scene )` lists
them. Your own code spawns one with `EntityService.Spawn( kind, new
EntitySpawn { User = user, Position = at }, out error )`, an item's `Use`
say, and removes one with `EntityService.Remove( go )`.

### Options: setting one up before it's placed

A kind with options lets the admin set it up in the spawn menu before
placing it: picking it in the Entities list shows your own panel under its
details, and every one they place (toolgun left click, Place at crosshair)
gets those options. Use on a placed one with the toolgun's Entities mode
opens the spawn menu on that one: your panel again, with Save, and your
buttons for it.

```csharp
public override bool HasOptions => true;
public override JsonObject DefaultOptions() => new() { ["seconds"] = 60 };
public override Panel CreateOptions( EntityOptionsEditor editor ) => new FlareOptions { Editor = editor };
public override string Summary( JsonObject options ) => $"burns {options["seconds"]} s";

public override GameObject Spawn( EntitySpawn spawn, out string error )
{
	var seconds = spawn.Options?["seconds"]?.GetValue<int>() ?? 60;   // null when the map's save lays one back in
	…
}

public override JsonObject ReadOptions( GameObject go ) => new() { ["seconds"] = go.Components.Get<Flare>().SecondsLeft };
public override void ApplyOptions( GameObject go, JsonObject options ) => go.Components.Get<Flare>().Light( (int)options["seconds"] );
public override IEnumerable<EntityAction> Actions( GameObject go ) => new[] { new EntityAction( "out", "Put it out", Danger: true ) };
public override string RunAction( GameObject go, string id, Pawn user ) { EntityService.Remove( go ); return "Flare put out."; }
```

The panel edits `editor.Json`: read it when you build, and again whenever
`editor.Version` changes (Discard, or the placed one changed), and set it
after every change. The gamemode does the rest: it keeps the admin's options
for new ones on their machine and on the host, sends a placed one's when
they press Save (`ApplyOptions`, on the host, permission checked, in the
audit log), runs your buttons (`RunAction`, host, what it returns is shown
to the admin), and adds Remove and Back to new ones. `ReadOptions` and
`Actions` are read on the admin's game, so build them from synced state.
`Summary` is the toolgun HUD's line for what a click places. None of it
needs network calls of your own.

A component implementing `IInteractable` makes any object usable with the
Use key: `InteractRange`, `Prompt( looker )` (what the prompt says to this
player; null for nothing to do, read on every player's game, so from synced
state) and `Use( user )`, which the gamemode runs on the host with the
range checked again.

## The map's own things

```csharp
var door = MapObjects.Find<Door>( "armoury_door" ).FirstOrDefault();
door.SetLocked( true );                       // nobody opens it, whatever their regiment
door.Open();                                  // opens it anyway, every part of it
MapObjects.Fire( "hangar_lights", "TurnOff" );
MapObjects.Send( MapObjects.Teleporter( "bridge" ), pawn );
MapObjects.SetText( screen, "Briefing at 1900", "Hangar 2" );
```

`MapObjects.All<T>()` lists every component of a type in the map (doors,
`MapButton`, `Teleporter`, `TextScreen`), `Find<T>( name )` the map entities
called that by the map's own wiring, and `Nearest<T>( position )` the
closest. `Fire` sends an input as the map would. A door's `IsLocked` is
synced, makes its prompt say Locked, and keeps out players and NPCs alike
until you unlock it; the map's own wiring and `Open()` still move it.
`SetText` keeps the screen's look, line by line. All host calls; nothing is
saved past the map.

## NPCs and vehicles

```csharp
var npc = NpcService.Spawn( NpcResource.Find( "outlaw_raider" ), position, yaw );
npc.Brain.GiveOrder( NpcOrderKind.GoTo, point, null );
var speeder = VehicleService.Spawn( VehicleResource.Find( "barc_speeder" ), position, rotation );
```

NPC orders are `Hold`, `Follow` (whoever you pass as the last argument),
`Patrol`, `Wander` and `GoTo`; `IServerEvents` hears NPCs and vehicles
spawn and die, and players get in and out.

A component marked `INpcAddon` is put on every NPC as it spawns, before the
network spawn, as `IPawnAddon` parts are on players, so its `[Sync]` state
reaches everyone. `this.GetNpc()` gives a part its NPC. The host runs NPCs:
change them there (`npc.Teleport`, `npc.Motor.Crouch( true )`). A restrained
NPC (`npc.Effects.ApplyRestraint( seconds )`) stands where it is and
doesn't move, aim or fire until it's let go.

A new way of moving a vehicle is a class deriving from `VehicleDrive`
(give it a `[Title]`). Once your addon is mounted, the Server Owner panel's
vehicle types get a **Drive** choice listing it; a vehicle type set to it
spawns with your drive instead of the gamemode's hover or flight. The drive
runs on the host: `ReadDriver()` gives the driver's keys and aim,
`UpdateIgnition`, `SetThrottle`, `Expect` and `CheckCrash` are the helpers
the gamemode's own drives use, and `Vehicle` and `Body` are the vehicle and
its rigidbody. Your drive derives from a gamemode component, so it can't
have `[Sync]` properties of its own: put synced state on a separate component.
If your addon is taken off, those vehicles go back to the gamemode's drive.

`VehicleDrive.KeyDown( rider, action )` reads any rider's key on the host.
The vehicle's `VehicleGuns` gives each gun's `OperatorOf` (its driver or
gunner), `AimOf` (how far it has turned and tipped), `MuzzlesNow` and
`ShotDirection`.

## Rules: changing what the gamemode decides

Rule interfaces let an addon change a decision the gamemode makes: who can
be hurt and how much, when and where players respawn, who may take a job,
promotions, names, shops. Each works two ways: as a **part on players**
(`IPawnAddon`), asked about the player it's on, or as a **standalone class**
anywhere in your addon (the manifest is a good home), asked about everyone.
Every method is optional, every call is guarded, and a standalone rule
object is made fresh once per frame, so keep nothing on it between calls.

```csharp
public sealed class SafeZones : IDamageRules
{
	public bool CanDamage( ICombatant attacker, ICombatant victim ) =>
		!(victim is Pawn pawn && Zones.Contains( pawn.WorldPosition ));

	public void ModifyDamage( DamageContext hit )
	{
		if ( hit.VictimPawn?.HasFlag( "addon.armour.heavy" ) == true && hit.IsExplosive )
			hit.Amount *= 0.5f;
	}
}
```

| Interface | Asked | What it decides |
|---|---|---|
| `IDamageRules` | host | `CanDamage( attacker, victim )`: whether a hit can land at all (also what bolts home on and NPCs go for). `ModifyDamage( DamageContext )`: change `Amount` or `Cancel()` each hit, attacks and falls/burning alike (`IsAttack`), before armour |
| `IRespawnRules` | host | `AdjustRespawnDelay( pawn, seconds )`, `AllowRespawn( pawn, out waitingFor )` (hold a downed player until a medic comes; their screen shows `waitingFor`), `ChooseSpawn( pawn, chosen, afterDeath )` (a cell, a medbay) |
| `IJobRules` | host and the job menu | `CanTakeJob( pawn, job, out reason )`: a locked job card shows the reason |
| `IRankRules` | host | `CanSetRank( actor, target, rank, out reason )` for promotions, demotions and sets; `CanStartTraining( trainer, target, training, regimentId, out reason )` |
| `INameRules` | host and the character screen | `NormalizeName( name )`; `IsValidName( name, out error )`: null leaves it to the gamemode, false refuses, true allows a name its rules wouldn't (brackets, say; 32 characters at most) |
| `IShopRules` | host and the shop screen | `CanBuy`, `AdjustPrice`, and your own currency: `HandlesPayment` for an entry, then `CanAfford`, `PriceLabel` ("1 token"), `TakePayment`, `Refund`; `OnBought` after |

Rules asked on players' games as well as the host (jobs, names, shops)
should read synced state, or answer yes on a client and decide on the host.

Downed players can be brought back by code: `pawn.ReviveInPlace( 0.5f )`
(where they lie, at half health; `IPawnEvents.OnRevived` hears it),
`pawn.RespawnNow()`, `pawn.SetRespawnIn( seconds )`.

## Your own powers, on-hit effects and weapon behaviours

Derive from `PowerBehaviour`, `OnHitEffect` or `WeaponBehaviour`, give it
an `Id` and a `Label`, and server owners pick it in the Server Owner panel:
a power with **Kind: Addon** and your **Behaviour**; a weapon's **On hit**
entry with **Kind: Addon**, your **Addon effect**, a **Strength** and
**Seconds**; a weapon's **Behaviour** (under Hooks). The gamemode keeps
doing everything else (slots, energy, cooldowns, ammo, fire rate, models,
sounds). All run on the host; a fresh instance each call.

```csharp
public sealed class HealBurst : PowerBehaviour
{
	public override string Id => "medic.burst";
	public override string Label => "Heal burst";
	public override void Cast( PowerCast cast )
	{
		foreach ( var ally in cast.Targets() )                // in the power's range and arc
			ally.Health.Heal( cast.Power.Force );
	}
}

public sealed class GrappleGun : WeaponBehaviour
{
	public override string Id => "grapple.gun";
	public override string Label => "Grappling hook";
	public override bool OnShot( WeaponShot shot )           // true: no bolt of its own
	{
		var hit = shot.Trace( 1500f );
		if ( hit.Hit ) shot.Pawn?.Movement.WalkTo( hit.HitPosition, speed: 600f );
		return true;
	}
}
```

`PowerCast` has the caster, the power (its Range, ArcDegrees, Force,
Damage, DurationSeconds), `Origin`, `Forward`, `Targets()` and
`Damage( target, amount )`. `OnHitContext` has the attacker, the target
(the victim, or the attacker for **Apply to: Self**), the weapon and the
entry (`Spec.Strength`, `Spec.Seconds`). `WeaponShot` has the shooter, the
weapon, the aim, the muzzle, `Trace( range )` and `Damage( health, amount )`;
`WeaponBehaviour` also hears `OnHit` (every hit of the weapon, any kind) and,
on every machine, `OnEquipped`, `OnHolstered` and `OnHeldUpdate`; with
`DrawsOwnImpacts => true` the gamemode leaves out its own hit sparks where
the weapon lands, for a weapon that draws its own. `CanFire( shot, out
reason )` can hold a shot before it goes: return false and nothing fires, no
ammo is spent, the trigger clicks and the player is told `reason` (the
taser won't fire at someone who can't be tased yet). It's asked on the
shooter's machine and again on the host, so read synced state. If your
addon is taken off, those powers do nothing, the effects are skipped and
the weapons fire as usual.

## NPC routines

```csharp
public sealed class Shopkeeper : NpcRoutine
{
	public override string Id => "market.shopkeeper";
	public override string Label => "Shopkeeper";
	public override void Tick( NpcRoutineContext c )      // host, a few times a second
	{
		if ( c.Motor.IsMoving ) return;
		var side = c.State.TryGetValue( "side", out var s ) ? (int)s : 1;
		c.State["side"] = -side;
		c.Motor.MoveTo( c.StandPoint( c.Post + Vector3.Left * 80f * side ), run: false );
	}
}
```

A routine is what an NPC does with no order and nothing to fight. Server
owners pick it as an NPC type's **Routine**; code sets it on one NPC with
`npc.Brain.RoutineId`. The context has the NPC, its `Motor` (`MoveTo`,
`Stop`, `IsMoving`, `LookAt`), its `Post` (home), `State` (notes kept on
that NPC) and `StandPoint( point )`. Fighting, orders and following still
come first.

## Chat channels and radio networks

```csharp
public sealed class Dispatch : AddonChatChannel
{
	public override string Id => "police.dispatch";
	public override string Label => "Dispatch";
	public override Color Colour => new( 0.3f, 0.7f, 1f );
	public override string[] Commands => new[] { "dispatch", "disp" };
	public override HashSet<ulong> Recipients( Pawn sender ) =>
		Players.All.Where( p => p.HasFlag( "addon.police.officer" ) ).Select( p => p.SteamId ).ToHashSet();
}
```

A chat channel gets its own command (listed and autocompleted like any),
chip and colour in the chat, and goes through the mute and the chat filters.
`CanSpeak( sender, out reason )` and `Permission` limit who uses it;
`AddonChatChannels.Send( id, pawn, text )` speaks on it from code.

A `RadioNetwork` (`Id`, `Label`, `Names`, `CanListen( pawn )`,
`CanTalk( pawn )`) is a radio channel beside Global, the regiment's and the
player's frequency: the radio cycles to it, `/channel <name>` picks it, and
voice and `/r` on it work as on any channel. It's asked on every machine,
so read synced state.

## Voice

```csharp
public sealed class Gag : Component, IPawnAddon, IVoiceRules
{
	[Sync( SyncFlags.FromHost )] public bool Gagged { get; set; }
	public VoiceHeard Hear( Pawn speaker, Pawn listener ) =>
		Gagged && speaker == this.GetPawn() ? VoiceHeard.Silent : VoiceHeard.Default;
}
```

A part with `IVoiceRules` says how one player hears another: `Default`
(distance and radio, as usual), `Silent`, `Near` (from where the speaker
stands, however far), `Radio` (in the ear with the radio sound) or `Clear`
(in the ear, like a phone call). Parts on the speaker and on the listener
are both asked, every frame, on both machines: any `Silent` wins, otherwise
the first answer that isn't `Default`.

## The camera and the gamemode's HUD

A part with `IPawnCameraModifier` changes its player's view every frame after
the gamemode places the camera: `view.Position`, `view.Rotation`,
`view.FieldOfView`, and `view.Camera` for anything else (cutscenes, a
turret sight, a shake). Aiming still goes from the player's eyes.

A part with `IHudRules` hides parts of the gamemode's own HUD on its
player's screen while `Hides( element )` says so: `Crosshair`,
`InteractPrompt`, `RadioIndicator`, `VehicleReticle`, `PlayerCard`,
`WeaponPanel`, `Hotbar`, `Powers`, `VoiceTalkers`, `VehicleHud`,
`DeathScreen`, `DamageFlash`, `DamageNumbers`, `Chat`, `Notifications`,
`Nameplates`. Pair it with an `AddonHudPanel` to replace one.

## The players list and the admin panel

Badges show on the Tab players list too (`BadgePlace.PlayerList`; the
default, `Everywhere`, includes it). An `IPlayerListColumn` (`Id`, `Title`,
`Order`, `IsVisible( viewer )`, `Value( pawn, viewer )`) adds a column to
every row; it's asked on the viewer's game, so read synced state.

An `AddonMenuTab` with `Host => AddonMenuHost.Admin` is a view in the admin
panel (K): under `AdminGroup` ("players", "moderation", "server", or its
own "addons" tab), for admins with its `Permission`. As with every panel,
the host must check whatever the panel asks it to do.

## Sounds and effects you can use

Your addon can play the gamemode's sounds and draw its fire, smoke and
explosions without shipping files of its own. Everything listed here is
always there: the gamemode's files come with the gamemode, and the rest is
part of s&box. A weapon package's sounds are there only when the server
mounts that package, so ship your own copy of any you need.

Sounds and effects play on the machine that calls them, for that player
only. So make them on every machine: from a component on a networked object
(one of your entities, or a part on a player), which runs everywhere, when
its synced state says so. A dedicated server has no screen or speakers, so
skip them there:

```csharp
public sealed class Barrel : Component
{
	[Sync( SyncFlags.FromHost )] public int Blasts { get; set; }   // the host adds one per blast
	int _shown = -1;

	protected override void OnUpdate()
	{
		if ( _shown < 0 ) _shown = Blasts;   // just arrived: don't replay old ones
		if ( Blasts == _shown ) return;
		_shown = Blasts;
		if ( Application.IsDedicatedServer ) return;

		Explosion.Spawn( WorldPosition, 0.6f, 40f, 0f, pieces: 0 );
		Explosion.Scorch( WorldPosition, 220f );
		GunAudio.Blast( "", WorldPosition );
	}
}
```

### Sounds

`"sounds/weapons/explosion.sound".PlayAt( position )` plays one at a spot;
`PlayOn( go, position )` plays one that moves with an object. Both give back
the engine's `SoundHandle`, to change its `Volume` or `Stop()` it.
`GunAudio.Blast( sound, position )` plays a bang that players far away hear
too (an empty sound is the gamemode's explosion).

The gamemode's:

| Path | What it is |
|---|---|
| `sounds/weapons/explosion.sound` | An explosion (`Explosion.DefaultSound`) |
| `sounds/weapons/rocket_flight.sound` | A rocket in flight |
| `sounds/weapons/bolt_flyby.sound` | A blaster bolt passing close |
| `sounds/weapons/bolt_hit_body.sound` | A bolt hitting a body (`GunAudio.BodyHit`) |
| `sounds/weapons/dry_fire.sound` | The trigger on an empty gun (`GunAudio.DryFire`) |
| `sounds/weapons/detonator_arm.sound`, `detonator_beep.sound`, `detonator_bounce.sound` | A thermal detonator armed, beeping, bouncing |
| `sounds/weapons/shell_in.sound`, `shell_rack.sound` | A shell loaded, a pump racked |
| `sounds/physgun/physgun_grab.sound`, `physgun_release.sound`, `physgun_freeze.sound`, `physgun_unfreeze.sound`, `physgun_hum.sound` | The physgun |
| `sounds/toolgun/toolgun_spawn.sound`, `toolgun_remove.sound` | The toolgun placing and removing |
| `sounds/radio/radio_on.sound`, `radio_off.sound` | The radio clicking on and off |
| `sounds/ui/notification.sound` | A notification |

s&box's:

| Path | What it is |
|---|---|
| `sounds/effects/fire/fire_burn_loop01.sound` | A fire crackling, 28 seconds; play it again when it ends (`VehicleFx.FireSound`) |
| `sounds/effects/explosion/explosion_small.sound` | A small explosion |
| `sounds/Impacts/Bullets/impact-bullet-<surface>.sound` | A bullet hitting `cloth`, `concrete`, `dirt`, `flesh`, `foliage`, `generic`, `glass`, `metal`, `plaster`, `plastic`, `sand`, `snow`, `water` or `wood` |
| `sounds/Impacts/Melee/impact-melee-<surface>.sound` | A melee hit on `cloth`, `concrete`, `dirt`, `flesh`, `glass`, `grass`, `metal`, `sand`, `snow` or `wood` |
| `sounds/footsteps/footstep-<surface>.sound` | A footstep on `concrete`, `dirt`, `glass`, `grass`, `metal` or `wood`; `concrete` and `wood` also have `-jump` and `-land` |
| `sounds/water/water_splash_medium.sound`, `water_bullet_impact.sound` | A splash, a shot into water |
| `sounds/kenney/ui/ui.button.press.sound` | UI clicks; the same folder has `ui.button.over`, `ui.button.deny`, `ui.navigate.forward`, `ui.navigate.back`, `ui.navigate.deny`, `ui.popup.message.open`, `ui.popup.message.close`, `ui.drag.start`, `ui.drag.stop` |

### Explosions, flashes and sparks

Ready-made, each one call:

| Call | What it draws |
|---|---|
| `Explosion.Spawn( centre, size, span, height )` | A full explosion: a white-hot flash, fireballs rolling into smoke, a rising cloud, sparks, burning pieces thrown out (`pieces`), a light and a shake for cameras close by. `size` 1 is a small vehicle, up to 4.5; a thermal detonator is about 0.6. `span` and `height` are how wide and tall what blew is. No sound: add `GunAudio.Blast` |
| `Explosion.Scorch( centre, radius )` | A burnt mark on the ground (or on the surface facing `normal`), which fades |
| `ImpactBurst.SpawnBlast( at, tint )` | A smaller blast: a flash, a light and sparks |
| `ImpactBurst.SpawnGlow( at, tint, size )` | A soft glow that pops and fades (the toolgun's) |
| `ImpactBurst.SpawnImpact( at, normal, tint )` | A hit spark on a surface |
| `ImpactBurst.SpawnMuzzle( at, forward, tint )` | A flash at a barrel |
| `Spark.Spawn( at, velocity, tint )` | One spark that flies and falls |

`Explosion.Spawn` draws about half as much for players who set Effects to
Low.

### Fire and smoke

`VehicleFx` names s&box's animated fire and smoke images, 64 frames each:
`Fireball` (rolling into dark smoke), `Flash` (a white-hot burst),
`Flames` (a loop), `SmokeCloud` and `SmokeBillow` (darker), plus `Ember`,
a still glowing dot. s&box has more in the same folders,
`textures/particles/flame/` and `textures/particles/smoke/`.
`VehicleFx.Particles( go, texture, max, additive )` makes a particle effect
drawn with one; `VehicleFx.Range`, `Curve` and `InDisc` fill in its values;
a `VehicleFx.Stream` feeds it a set number a second from a point that can
move. `VehicleFx.Object` makes an object for it that isn't networked or
saved, and `VehicleFx.Low` says the player chose fewer particles.

A steady fire, as on a burning wreck:

```csharp
public sealed class Campfire : Component
{
	VehicleFx.Stream _flames;
	SoundHandle _crackle;

	protected override void OnStart()
	{
		if ( Application.IsDedicatedServer ) return;
		var go = VehicleFx.Object( "Campfire flames", WorldPosition );
		var fx = VehicleFx.Particles( go, VehicleFx.Flames, 24, additive: false, loopsPerSecond: 0.45f );
		fx.Lifetime = VehicleFx.Range( 0.9f, 1.5f );
		fx.Tint = new Color( 1f, 0.85f, 0.7f );
		fx.Brightness = 1.7f;
		fx.Alpha = VehicleFx.Curve( (0f, 0f), (0.15f, 1f), (0.7f, 0.85f), (1f, 0f) );
		fx.Scale = VehicleFx.CurveRange( [(0f, 75f), (0.3f, 115f), (1f, 90f)], [(0f, 95f), (0.3f, 150f), (1f, 115f)] );
		fx.ApplyRotation = false;
		_flames = new VehicleFx.Stream( fx ) { Spread = 12f };
		_flames.StandOnPoint( VehicleFx.FlameFoot );   // the flames grow up from the point
	}

	protected override void OnUpdate()
	{
		if ( _flames is null ) return;
		_flames.Feed( 5f, WorldPosition );   // flames a second
		if ( _crackle is null || _crackle.IsStopped || _crackle.Finished )
			_crackle = VehicleFx.FireSound.PlayOn( GameObject, WorldPosition );
	}

	protected override void OnDestroy()
	{
		_crackle?.Stop();
		_flames?.Object.Destroy();
	}
}
```

A one-off puff is the same effect fed by hand: `fx.Emit( position, 0f )`
for each particle, with its own `Velocity`, on an object that removes
itself (`go.Components.Create<AutoDestroy>().Lifetime = 5f`). A
`FadingLight` (`Radius`, `Peak`, `Seconds`) adds a light that flares and
fades.

## Running and testing

Publish the package unlisted, run the gamemode from its editor or a
dedicated server, and add your ident on the Mounts tab. This is how players
get it, so what you see this way is what they'll see.

The server log prints `[addon] Flares 1.0 (yourorg.flares) loaded: SDK 1`
when the code is in, `[addon] …` warnings for anything dropped or failing,
and `[mount] yourorg.flares mounted — contributed N GWRP resources` for the
files. The Mounts tab names the addon under its row. If a player's log says
their game couldn't get the code for your package, their game failed to
compile it; it compiles on every machine under the same rules as in your
editor, so this is rare, and the log says what failed.

## Publishing an addon

What goes up is your compiled code (never the source) and the files in
`Assets/`, as for any package. The editor's Publish wizard can't publish an
addon with a parent game at the moment: sbox.game compiles each package
again on its servers before accepting it, and that compile can't find the
parent (Facepunch's
[sbox-public #12172](https://github.com/Facepunch/sbox-public/issues/12172)).
The template's `Tools/publish_without_server_check.cs` does what the wizard
does and sends the final request without that check (the README's
Publishing section says how to run it); servers and players compile the
code themselves when they mount the package, which is what they do anyway.
Use the wizard again once the issue is closed. The first publish is unlisted: set up the page, test it mounted,
make it public. Each later publish is a new revision, which servers get at
their next restart.

## Rules for addons

- **Prefix every id with your slug:** item ids, command names, entity kind
  and help tab ids, settings ids and permission keys (`addon.<slug>.…`).
  The gamemode's own ids always win a clash; between addons the first by
  type name does, and the loser is dropped with a warning.
- **Stay out of the `SWRP` namespace**, and give classes names unlikely to
  clash with another addon's.
- **Keep asset extensions short:** five letters or fewer.
- **Derive from `ItemResource`, never from `BasicItem`** (the gamemode's own
  item type), and from `Component` directly for anything with `[Sync]`.
- **The host decides.** Each hook says where it runs; game state changes on
  the host, and what others need to see is `[Sync]`ed. The gamemode owns
  the network calls behind every surface (commands, settings, entities), so
  an addon needs none of its own for them.
- **No static caches** of types or instances found at runtime; re-read on
  each use, as the gamemode does, so hotloads and live mounts work.
- **Keep hooks cheap:** `IsVisible`, `Blocks`, `SpeedMultiplier`, `Prompt`,
  `UsePrompt`, `Badges` and `OnHeldUpdate` run every frame.
- **Don't ship scenes**, as for any package.
- **A hook that throws is logged once** per hook and skipped from then on.
- **Watch `GwrpSdk.Version`:** the template's CHANGELOG says what changed
  in each; recompile against the latest gamemode when it moves.
