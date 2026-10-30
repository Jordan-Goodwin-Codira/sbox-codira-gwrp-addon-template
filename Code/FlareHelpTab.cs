using Sandbox.UI;
using Sandbox.UI.Construct;

namespace MyOrg.Flares;

/// <summary>A tab on the help screen (H), after the gamemode's own.</summary>
public sealed class FlareHelpTab : SWRP.UI.AddonMenuTab
{
	public override string Id => "flares.help";
	public override string Title => "Flares";

	public override Panel Create()
	{
		var panel = new Panel();
		panel.Add.Label( "Flares", "section-title" );
		panel.Add.Label( "Use a flare from your hotbar to light one where you aim. Press Use on a burning flare to stamp it out.", "hint" );
		panel.Add.Label( "Hold Jump while falling to glide down on your pack.", "hint" );
		return panel;
	}
}
