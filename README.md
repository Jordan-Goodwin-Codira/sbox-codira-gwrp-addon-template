# GWRP addon template

A starting point for an addon with code for
[Galaxy Wars RP](https://sbox.game/codira/starwarsrp), the s&box roleplay
gamemode. An addon is an ordinary sbox.game package that a server owner
mounts; its code runs on the server and on every player's game, and the
gamemode finds what it adds by the base classes it derives from. Nothing in
the gamemode changes.

An addon can add:

- **Items** with behaviour of their own (`ItemResource`): what using them
  does, what menus say about them, what happens when they're equipped,
  gained, lost or dropped on death.
- **Parts on every player** (`IPawnAddon`): a component on each player's
  object with synced state, that can stop them doing things
  (`IActionBlocker`) or change their speed (`IMovementModifier`).
- **Chat commands** (`ChatCommand`), listed in the help screen and the
  chat's command list.
- **Admin commands** (`AdminPlayerCommand`) on the admin panel's Players
  tab, with their own permissions in the role editor.
- **Server settings** (`AddonSettings`) on the Server Owner panel's Settings
  tab, saved on the server and sent to every player.
- **HUD panels** (`AddonHudPanel`) and **help-screen tabs** (`AddonMenuTab`).
- **Things in the world** (`EntityKind`): placed from the spawn menu and the
  toolgun, moved with the physgun, undone with Z and saved with the map;
  and objects players use with the Use key (`IInteractable`).

The full reference is [GUIDE.md](GUIDE.md), one section per surface.

## What's in the example

The example addon is **Flares**: a hand flare players throw, a flare admins
place, and a glide pack every player wears. One file per surface, each short
enough to read in a minute:

| File | What it shows |
|---|---|
| `Code/FlaresAddon.cs` | The manifest: the addon's id, name, version, SDK version and permissions. Every addon has one. |
| `Code/FlareItem.cs` | An item type with its own file extension (`.flare`) and fields; using it lights a flare where you aim. |
| `Assets/gwrp_addon/gameresources/items/flare.flare` | An item of that type, as the editor saves it. |
| `Code/FlareKind.cs` | A kind of entity admins can place, and the `Flare` component on it: it burns down on the host and players stamp it out with Use. |
| `Code/FlaresCommand.cs` | `/flares`: a chat command. |
| `Code/LightFlareOnCommand.cs` | "Light a flare": an admin command with an option, behind the permission `addon.flares.light`. |
| `Code/FlareSettings.cs` | Two settings on the Server Owner panel's Settings tab. |
| `Code/UI/FlareHud.razor` | A HUD panel counting the flares burning, styled with the gamemode's classes. |
| `Code/FlareHelpTab.cs` | A tab on the help screen. |
| `Code/GlidePack.cs` | A part on every player: hold Jump while falling to glide. |
| `Tools/publish_without_server_check.cs` | A script that publishes the package while sbox.game's own compile check is broken for addons (see Publishing). Not part of the addon: s&box compiles only `Code/` and `Editor/`. |

Delete what you don't need and rename the rest. Everything the gamemode
looks for is found by base class, so no list anywhere needs updating.

## Start here

1. **Install the s&box editor** (free from sbox.game).
2. **Make your repository from this one**: on GitHub, **Use this template**,
   or download it.
3. **Add the project** in the s&box launcher: Projects, add an existing
   project, pick `gwrp_addon.sbproj`. The first open downloads the gamemode
   (`codira.starwarsrp`, the project's **ParentPackage**) and compiles your
   code against it, so the editor knows `ItemResource`, `GwrpAddon` and the
   rest without the gamemode's source.

   **For now, open it on s&box's staging branch.** On the normal branch the
   editor stops at "Loading parent package" when it opens an addon project
   ([sbox-public #12225](https://github.com/Facepunch/sbox-public/issues/12225)).
   In Steam, right-click s&box, **Properties**, **Betas**, and pick
   **staging**; switch back to **None** once the issue is closed. Players
   and servers don't need it.
4. **Make it yours.** In `gwrp_addon.sbproj`, set **Title** (32 characters
   at most), **Org** (your sbox.game organisation; `local` can't be
   published) and **Ident** (your slug, like `flares`). Rename
   `Assets/gwrp_addon/` to your slug. In the code, change the namespace
   `MyOrg.Flares` to your own (the manifest's namespace is what marks the
   types as yours) and `FlaresAddon.Id` to `org.ident`.
5. **Run it** (next section), **edit or delete the examples**, and
   **publish** (below). A server owner then mounts your package by its
   ident on the Server Owner panel's Mounts tab.

## Running it

Publish the package unlisted (below), run the gamemode from its editor or
a dedicated server, and mount your ident on the Mounts tab. This is how
players will get it, so what you see this way is what they'll see: the
server log prints `[addon] Flares 1.0 (org.ident) loaded: SDK 1` when the
code is in, and the Mounts tab names the addon under its row.

## Publishing

The editor's Publish wizard can't publish an addon with a parent game at
the moment: sbox.game compiles each package again on its servers before
accepting it, and that compile can't find the parent (Facepunch's
[sbox-public #12172](https://github.com/Facepunch/sbox-public/issues/12172)).
Until that's fixed, publish with `Tools/publish_without_server_check.cs`,
which does everything the wizard does and sends the final request without
that check; servers and players compile the code themselves when they
mount the package, which is what they do anyway.

1. Set your **Org** and **Ident** in the `.sbproj` (an addon under `local`
   can't be published), and the version at the top of the script.
2. Open the project in the s&box editor. The template has the editor's
   connection for tools switched on (`EnableMcpCommunication` in the
   `.sbproj`), which the next step needs.
3. In a terminal, find the editor's process id (Task Manager's Details tab,
   `sbox-dev.exe`) and run, with your own paths:

   ```
   "C:\Program Files (x86)\Steam\steamapps\common\sbox\bin\managed\sbox-cli.exe" --connect <editor pid> --request-timeout 600 --call run_csharp "{\"path\":\"C:/path/to/your/addon/Tools/publish_without_server_check.cs\"}"
   ```

   It prints `PUBLISHED <your ident> <version>` when it's done, or what
   stopped it.

Use the wizard again once the issue is closed.

The first publish makes the package unlisted. Set its page up on sbox.game
(description, thumbnail), test it mounted on a server, then make it public.
Each later publish is a new revision; servers running it get it at their
next restart.

## Keeping up with the gamemode

The SDK has a version, `GwrpSdk.Version`, which goes up when a surface
changes in a way an addon built against an older one could notice;
[CHANGELOG.md](CHANGELOG.md) lists what changed in each. Your manifest's
`SdkVersion => GwrpSdk.Version` is baked in when you compile, so a server on
a newer SDK sees the difference and prints a warning (on the Mounts tab too)
but still loads your addon. When it changes, open the project (it compiles
against the latest gamemode), fix anything that no longer compiles, and
publish again.

## Rules worth knowing

- **Prefix every id with your slug**: item ids, command names, entity kind
  ids, settings ids, permission keys (`addon.<slug>.<thing>`). Two addons
  with the same id can't both be in; the gamemode's own always win.
- **Asset extensions are 11 characters at most** (`flare`, `handcuffs`);
  longer ones silently never compile.
- **Derive from `ItemResource`, never `BasicItem`**, and from `Component`
  directly for anything with `[Sync]` state: the engine can't generate sync
  code through a base class from another package.
- **The host decides.** Hooks say which machine they run on; anything that
  changes the game happens on the host, and state others need to see is
  `[Sync]`ed.
- **Keep nothing static** that holds types or instances found at runtime:
  the gamemode re-reads what's loaded on each use so edits hotload.
- **Don't ship scenes** in your package: every scene in a mounted package
  is loaded when it's mounted, with everything it places.
- **A hook that throws is logged once** and the player sees "X failed; the
  server log has more"; the game carries on without it.

[GUIDE.md](GUIDE.md) has the full reference, including what server owners
see of an addon: its row on the Mounts tab, its permissions in the role
editor, its commands in the admin panel and its section on the Settings
tab.

## Licence

MIT: use it for anything, keep the licence notice. The engine's development
models the example uses are s&box's own; see [CREDITS.md](CREDITS.md).
