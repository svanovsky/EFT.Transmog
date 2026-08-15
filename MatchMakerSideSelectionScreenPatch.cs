using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Matchmaker;
using HarmonyLib;
using PlayerIcons;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Transmog
{
	public class MatchMakerSideSelectionScreenPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			// ISession was renamed to IEftSession in SPT 4.1.
			var method = AccessTools.Method(typeof(MatchMakerSideSelectionScreen), "Show", new []
			{
				typeof(IEftSession), typeof(RaidSettings), typeof(IHealthController), typeof(InventoryController)
			});
			if (method != null)
				Plugin.LogInfo("Found MatchMakerSideSelectionScreen Show method.");
			else
				Plugin.LogError("Unable to find MatchMakerSideSelectionScreen Show method.");
			return method;
		}

		public static Profile TryMarkScavProfile(Profile scavProfile)
		{
			if (Plugin.DisableScavTransmogInLobby.Value)
			{
				var clone = scavProfile.Clone();
				TacticalClothingViewPatch.MarkProfile(clone);
				return clone;
			}
			return scavProfile;
		}

		[PatchTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Targeting "_savageProfile" directly (rather than the old obfuscated "profile_1" plus a
			// first-match guard) since SPT 4.1's deobfuscation gives the scav-profile field its real name,
			// matching what this transpiler was always trying to intercept.
			var hasFound = false;
			foreach (var codeInstruction in instructions)
			{
				if (!hasFound && codeInstruction.opcode == OpCodes.Stfld &&
				    codeInstruction.operand is FieldInfo field &&
				    field.Name == "_savageProfile")
				{
					hasFound = true;
					yield return new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(MatchMakerSideSelectionScreenPatch), nameof(TryMarkScavProfile)));
				}
				yield return codeInstruction;
			}
		}
	}
}