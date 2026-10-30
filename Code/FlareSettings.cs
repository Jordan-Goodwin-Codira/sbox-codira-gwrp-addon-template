namespace MyOrg.Flares;

/// <summary>
/// Settings on the Server Owner panel's Settings tab, one section per class.
/// The host saves them in the data folder (server/addons/flares.json) and
/// sends them to every player; read them anywhere with
/// AddonSettingsStore.Get&lt;FlareSettings&gt;().
/// </summary>
public sealed class FlareSettings : AddonSettings
{
	public override string Id => "flares";
	public override string Title => "Flares";
	public override string Description => "How flares placed by admins behave.";

	/// <summary>How long a flare placed from the spawn menu or toolgun burns.</summary>
	[Property, Title( "Burn seconds" ), Range( 5f, 300f )]
	public float DefaultBurnSeconds { get; set; } = 60f;

	/// <summary>Whether the HUD counts the flares burning on the map.</summary>
	[Property, Title( "Show the flare count" )]
	public bool ShowCount { get; set; } = true;

	public override void OnChanged() => Log.Info( $"[flares] settings: {DefaultBurnSeconds:0} s, count {(ShowCount ? "shown" : "hidden")}" );
}
