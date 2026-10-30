using System.Text.Json.Nodes;

namespace MyOrg.Flares;

/// <summary>
/// A kind of thing admins can place in the world. It appears in the spawn
/// menu's Entities list and the toolgun's Entities mode, with its own
/// permission (admin.entity unless you say otherwise); the gamemode spawns
/// it over the network, tracks it for undo, the physgun and /savemap, and
/// asks Save and Load for what to keep in the map's save.
/// </summary>
public sealed class FlareKind : EntityKind
{
	public const string KindId = "flares.flare";

	public override string Id => KindId;
	public override string Label => "Flare";
	public override string Description => "A burning flare that lights the ground around it. Players stamp it out with Use.";
	public override string PreviewModel => "models/dev/sphere.vmdl";

	/// <summary>Host: build the object. Don't spawn it over the network; the gamemode does.</summary>
	public override GameObject Spawn( EntitySpawn spawn, out string error )
	{
		error = "";
		var go = new GameObject( true, "Flare" );
		go.WorldPosition = spawn.Position;
		go.WorldRotation = spawn.Rotation;
		go.WorldScale = 0.25f;
		var renderer = go.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( "models/dev/sphere.vmdl" );
		renderer.Tint = new Color( 1f, 0.35f, 0.1f );
		var collider = go.Components.Create<SphereCollider>();
		collider.Radius = 16f;
		var light = go.Components.Create<PointLight>();
		light.LightColor = new Color( 1f, 0.45f, 0.15f ) * 4f;
		light.Radius = 400f;
		var flare = go.Components.Create<Flare>();
		flare.Light( AddonSettingsStore.Get<FlareSettings>()?.DefaultBurnSeconds ?? 60f );
		return go;
	}

	public override JsonObject Save( GameObject entity ) =>
		new JsonObject { ["seconds"] = entity.Components.Get<Flare>()?.SecondsLeft ?? 0 };

	public override void Load( GameObject entity, JsonObject data )
	{
		if ( entity.Components.Get<Flare>() is { } flare && data?["seconds"]?.GetValue<int>() is { } seconds ) flare.Light( seconds );
	}

	public override string Describe( GameObject entity ) =>
		$"Flare ({entity.Components.Get<Flare>()?.SecondsLeft ?? 0} s left)";
}

/// <summary>
/// The flare itself: a component on the spawned object. It burns down on the
/// host, and players can stamp it out with Use, which the gamemode offers
/// through IInteractable (the prompt is read on every player's game, so what
/// it shows is synced).
/// </summary>
public sealed class Flare : Component, IInteractable
{
	[Sync( SyncFlags.FromHost )] public int SecondsLeft { get; set; }

	TimeSince _sinceTick;

	/// <summary>Host: start (or restart) the burn.</summary>
	public void Light( float seconds ) => SecondsLeft = (int)Math.Max( 1f, seconds );

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost || _sinceTick < 1f ) return;
		_sinceTick = 0;
		SecondsLeft--;
		if ( SecondsLeft <= 0 ) EntityService.Remove( GameObject );
	}

	public float InteractRange => 96f;

	public string Prompt( Pawn looker ) => $"Stamp out the flare ({SecondsLeft} s left)";

	/// <summary>Host: a player pressed Use on it.</summary>
	public void Use( Pawn user )
	{
		NotificationService.Notify( user.SteamId, "You stamp the flare out." );
		EntityService.Remove( GameObject );
	}
}
