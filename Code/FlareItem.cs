namespace MyOrg.Flares;

/// <summary>
/// An item players carry and use: deriving from ItemResource gives it a
/// file type of its own (here .flare), every field an item has, and a place
/// in kits, shops, inventories and the Server Owner panel. The gamemode
/// calls Use on the host when a player clicks with it in hand.
/// </summary>
[AssetType( Name = "Flare", Extension = "flare", Category = "Flares" ), Title( "Flare" )]
public sealed class FlareItem : ItemResource
{
	/// <summary>How long a flare this item lights burns for.</summary>
	[Property, Title( "Burn seconds" ), Group( "Flare" ), Range( 5f, 300f )]
	public float BurnSeconds { get; set; } = 60f;

	/// <summary>How far in front of the player it can be thrown.</summary>
	[Property, Title( "Throw range" ), Group( "Flare" )]
	public float ThrowRange { get; set; } = 400f;

	public override bool HasUseAction => true;
	public override string KindLabel => "Flare";
	public override string UseSummary => $"Lights a flare for {BurnSeconds:0} s";
	public override string WhatItDoes() => $"Drops a burning flare where you aim, up to {ThrowRange:0} units away.";
	public override bool CanUseFromMenu => false;

	public override bool Use( ItemUse use, out string error )
	{
		var trace = use.Trace( ThrowRange );
		var at = trace.Hit ? trace.HitPosition + trace.Normal * 4f : use.Origin + use.Direction * ThrowRange;
		var flare = EntityKinds.Find( FlareKind.KindId );
		if ( flare is null ) { error = "The flares addon isn't mounted."; return false; }
		var spawned = EntityService.Spawn( flare, new EntitySpawn { User = use.User, Position = at }, out error );
		if ( !spawned.IsValid() ) return false;
		spawned.Components.Get<Flare>()?.Light( BurnSeconds );
		use.Notify( "Flare lit." );
		return true;
	}
}
