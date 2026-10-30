namespace MyOrg.Flares;

/// <summary>
/// A part on every player: a component marked IPawnAddon is created on each
/// player's object before it's spawned over the network, so its [Sync] state
/// reaches everyone. Derive from Component directly (not from a gamemode
/// class) or the engine can't generate the [Sync] code. This one is a glide
/// pack: hold Jump while falling and you fall slowly. The player's own
/// machine simulates their body, so the glide runs there; it also blocks
/// attacking while gliding, through IActionBlocker, and halves walking speed
/// for a moment after landing, through IMovementModifier.
/// </summary>
public sealed class GlidePack : Component, IPawnAddon, IActionBlocker, IMovementModifier
{
	const float FallSpeed = 90f;

	[Sync] public bool Gliding { get; set; }

	TimeSince _sinceLanding = 10f;

	protected override void OnFixedUpdate()
	{
		if ( !this.IsLocalPawn() ) return;
		var pawn = this.GetPawn();
		if ( !pawn.IsValid() || !pawn.Controller.IsValid() ) return;
		var falling = pawn.Controller.IsAirborne && pawn.Velocity.z < -FallSpeed;
		var wants = falling && Input.Down( "Jump" ) && pawn.Can( PawnAction.Move );
		if ( Gliding && !pawn.Controller.IsAirborne ) _sinceLanding = 0;
		Gliding = wants;
		var body = Components.Get<Rigidbody>();
		if ( !wants || !body.IsValid() || !body.PhysicsBody.IsValid() ) return;
		body.PhysicsBody.Velocity = body.PhysicsBody.Velocity.WithZ( -FallSpeed );
	}

	public bool Blocks( PawnAction action, out string reason )
	{
		reason = Gliding ? "Not while you're gliding." : "";
		return Gliding && action == PawnAction.Attack;
	}

	public float SpeedMultiplier => _sinceLanding < 1f ? 0.5f : 1f;
}
