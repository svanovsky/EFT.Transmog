using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.UI;

namespace Transmog
{
	public class PlayerModelViewPatch : ModulePatch
	{
		// Was LastPlayerStateClass in SPT 4.0; SPT 4.1's fuller client deobfuscation reverted the name
		// back to PlayerVisualRepresentation (its name in SPT 3.9 / EFT 0.14.9).
		protected override MethodBase GetTargetMethod()
		{
			foreach (var method in typeof(PlayerModelView).GetMethods())
			{
				var parameters = method.GetParameters();
				if (method.Name == "Show" &&
					parameters.Length == 6 &&
				    parameters[0].ParameterType == typeof(PlayerVisualRepresentation))
				{
					Plugin.LogInfo("Found PlayerModelView Show method.");
					return method;
				}
			}
			Plugin.LogError($"Unable to find PlayerModelView Show method.");
			return null;
		}

		[PatchPrefix]
		public static void Prefix(ref PlayerVisualRepresentation __0)
		{
			Plugin.LogDebug("On PlayerModelViewPatch Show()");
			if (Plugin.GetPlayerPmcProfile() == null)
			{
				Plugin.LogDebug("Skip side selection view");
				return;
			}
			if (__0.Info.Nickname == TacticalClothingViewPatch.MAGIC_ID_SKIP_TRANSMOG)
			{
				Plugin.LogDebug("Skip clothing shopping view");
				return;
			}
			var playerName = __0.Info.Side == EPlayerSide.Savage
				? Plugin.GetPlayerScavProfile().GetCorrectedNickname()
				: Plugin.GetPlayerPmcProfile().Nickname;
			Plugin.LogDebug($"Check {__0.Info.CorrectedNickname} vs {playerName}");
			if (__0.Info.CorrectedNickname != playerName)
			{
				Plugin.LogDebug("Not same one");
				return;
			}
			Plugin.LogDebug("Is player");
			var newVisual = new PlayerVisualRepresentation(__0.Info, __0.Customization, Plugin.CloneAndModifyEquipmentClass(__0.Equipment, __0.Info.Side == EPlayerSide.Savage));
			__0 = newVisual;
		}
	}
}