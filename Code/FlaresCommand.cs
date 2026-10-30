namespace MyOrg.Flares;

/// <summary>
/// A chat command: /flares says how many flares are burning. It's listed in
/// the help screen and the chat's command list, and runs on the host. A name
/// that clashes with one of the gamemode's own commands is dropped.
/// </summary>
public sealed class FlaresCommand : ChatCommand
{
	public override ChatCommandInfo Info => new( "Flares", new[] { "flares" }, "", "Say how many flares are burning, and how far the nearest is." );

	public override void Run( Pawn sender, string args )
	{
		var flares = EntityService.All( sender.Scene ).Where( e => e.KindId == FlareKind.KindId ).ToList();
		if ( flares.Count == 0 ) { NotifyInfo( sender, "No flares are burning." ); return; }
		var nearest = flares.Min( f => f.WorldPosition.Distance( sender.WorldPosition ) );
		NotifyInfo( sender, $"{flares.Count} flare{(flares.Count == 1 ? "" : "s")} burning; the nearest is {nearest:0} units away." );
	}
}
