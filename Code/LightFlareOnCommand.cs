namespace MyOrg.Flares;

/// <summary>
/// An admin command: a button on the admin panel's Players tab, with its
/// options, run on the host once the gamemode has checked the admin has the
/// permission and outranks the target. The id is prefixed with the addon's
/// slug so it can't clash with another addon's.
/// </summary>
public sealed class LightFlareOnCommand : AdminPlayerCommand
{
	public override AdminCommand Info => new()
	{
		Id = "flares.light",
		Name = "Light a flare",
		Glyph = "🔥",
		Permission = "addon.flares.light",
		Description = "Light a flare at the player's feet.",
		Params = new[] { new CommandParam { Key = "seconds", Label = "Burn seconds", Type = "number", Placeholder = "60" } },
	};

	public override bool Run( AdminCommandRun run )
	{
		var kind = EntityKinds.Find( FlareKind.KindId );
		if ( kind is null ) return run.Fail( "The flares addon isn't mounted." );
		var spawn = new EntitySpawn { User = run.Actor, Position = run.Target.WorldPosition + Vector3.Up * 4f };
		var flare = EntityService.Spawn( kind, spawn, out var error );
		if ( !flare.IsValid() ) return run.Fail( error );
		var seconds = run.FloatArg( "seconds", 60f );
		flare.Components.Get<Flare>()?.Light( seconds );
		run.TellTarget( $"{run.Actor.DisplayName} lit a flare at your feet." );
		run.AuditDetails = $"{seconds:0} s";
		return true;
	}
}
