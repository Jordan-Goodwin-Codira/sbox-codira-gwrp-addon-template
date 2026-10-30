namespace MyOrg.Flares;

/// <summary>
/// The addon's manifest: one of these per package with code. The gamemode
/// finds it when the package is mounted, names the addon on the Mounts tab
/// and the help screen, and treats every type in this namespace as the
/// addon's. Change the id to your package's ident (org.name) and the
/// namespace to your own; the rest of the example follows the namespace.
/// </summary>
public sealed class FlaresAddon : GwrpAddon
{
	public override string Id => "local.gwrp_addon";
	public override string Name => "Flares";
	public override string Version => "1.0";
	public override int SdkVersion => GwrpSdk.Version;

	/// <summary>The permissions the addon checks, so the role editor can grant them.</summary>
	public override IEnumerable<PermissionDef> Permissions => new[]
	{
		new PermissionDef { Key = "addon.flares.light", Description = "Light a flare on any player from the admin panel" },
	};

	public override void OnLoaded() => Log.Info( "[flares] loaded" );
}
