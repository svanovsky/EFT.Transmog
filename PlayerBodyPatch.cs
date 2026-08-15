using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using SPT.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;

namespace Transmog
{
	public class PlayerBodyPatch
	{
		public void Enable()
		{
			new PlayerBodyPatch1().Enable();
			new PlayerBodyPatch2().Enable();
			new PlayerBodyPatch3().Enable();
			new PlayerBodyPatch4().Enable();
		}

		private class PlayerBodyPatch1 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				var method = AccessTools.FirstMethod(typeof(PlayerBody), info => info.Name == "Init" && 
					info.GetParameters().Length == 8);
				if (method != null)
					Plugin.LogInfo("Found PlayerBody Init method.");
				else
					Plugin.LogError("Unable to find PlayerBody Init method.");
				return method;
			}

			[PatchPrefix]
			public static void Prefix(ref InventoryEquipment __1, string __5)
			{
				// Fix game freezing on side selection
				if (Plugin.GetPlayerPmcProfile() == null)
				{
					Plugin.LogDebug("Skip side selection view");
					return;
				}

				Plugin.GetPlayerScavProfile();

				if (Plugin.costumes.TryGetValue(__5, out Plugin.CostumeItem[] costumes))
				{
					Plugin.LogDebug($"Found local player's PlayerBody.");
					if (Plugin.AffectIngameModel.Value || (__5 != Plugin.PmcId && __5 != Plugin.ScavId))
					{
						var newEquipments = __1.CloneVisibleItem();

						foreach (var visualSlot in InventoryEquipment.AllVisualSlotNames)
						{
							if (Plugin.ToSlotType(visualSlot, out var slotType) && costumes[(int)slotType].HasEffect())
							{
								Plugin.LogDebug($"Apply transmog to {slotType} slot.");
								costumes[(int)slotType].AffectSlot(newEquipments.GetSlot(visualSlot));
							}
						}

						__1 = newEquipments;
					}
				}
			}

			[PatchPostfix]
			public static void Postfix(PlayerBody __instance)
			{
				Plugin.LogDebug(__instance.GetSlotViewsDebugString());
			}
		}

		private class PlayerBodyPatch2 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				var method = AccessTools.Method(typeof(Player), "Init");
				if (method == null)
				{
					Plugin.LogError("Unable to find Player Init method.");
					return null;
				}
				var asyncStateMachineAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
				var realMethod = asyncStateMachineAttribute?.StateMachineType?.GetMethod("MoveNext", AccessTools.allDeclared);
				if (realMethod == null)
				{
					Plugin.LogError($"Found Player Init method, but cannot find its real method.");
					return null;
				}
				Plugin.LogDebug($"Found Player Init method " +
				                $"and its real method: {asyncStateMachineAttribute.StateMachineType.FullName}.{realMethod.Name}");
				return realMethod;
			}

			[PatchTranspiler]
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				foreach (var codeInstruction in instructions)
				{
					if (codeInstruction.opcode == OpCodes.Ldstr &&
					    codeInstruction.operand?.ToString() == "")
					{
						yield return new CodeInstruction(OpCodes.Ldloc_1);
						yield return new CodeInstruction(OpCodes.Call,
							AccessTools.PropertyGetter(typeof(Player), "Profile"));
						yield return new CodeInstruction(OpCodes.Call,
							AccessTools.PropertyGetter(typeof(Profile), "ProfileId"));
						Plugin.LogInfo("PlayerBodyPatch2 Patched.");
						continue;
					}
					yield return codeInstruction;
				}
			}
		}

		/// <summary>
		/// Tell the game to load the resources of transmog.
		/// </summary>
		private class PlayerBodyPatch3 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				var method = AccessTools.Method(typeof(Profile), "GetAllPrefabPaths");
				if (method != null)
					Plugin.LogInfo("Found Profile GetAllPrefabPaths method.");
				else
					Plugin.LogError("Unable to find Profile GetAllPrefabPaths method.");
				return method;
			}

			[PatchPostfix]
			public static IEnumerable<ResourceKey> Postfix(IEnumerable<ResourceKey> __result, Profile __instance)
			{
				foreach (var key in __result)
				{
					yield return key;
				}

				if (!Plugin.costumes.TryGetValue(__instance.Id, out Plugin.CostumeItem[] costumes)) yield break;
				Plugin.LogDebug($"Append transmog resources to {__instance.GetCorrectedNickname()}.");
				foreach (var equipment in costumes)
				{
					var item = equipment.Get();
					if (item != null)
					{
						foreach (var key in item.Template.AllResources)
						{
							yield return key;
						}
					}
				}
			}
		}

		/// <summary>
		/// Fix game freezing when players who hiding their backpack take exercise.
		/// </summary>
		private class PlayerBodyPatch4 : ModulePatch
		{
			protected override MethodBase GetTargetMethod()
			{
				// was method_16 in SPT 3.8, method_27 in SPT 3.9, method_34 in SPT 4.0;
				// SPT 4.1's client deobfuscation gave it its real name.
				var method = AccessTools.Method(typeof(HideoutPlayerOwner), "HideSkinsForGym", new [] { typeof(bool) });
				if (method != null)
					Plugin.LogInfo("Found HideoutPlayerOwner HideSkinsForGym method.");
				else
					Plugin.LogError("Unable to find HideoutPlayerOwner HideSkinsForGym method.");
				return method;
			}

			[PatchPrefix]
			public static bool Prefix(HideoutPlayerOwner __instance, bool __0)
			{
				__instance.HideoutPlayer.PlayerBones.HolsterPrimary.gameObject.SetActive(!__0);
				__instance.HideoutPlayer.PlayerBones.HolsterSecondary.gameObject.SetActive(!__0);
				if (__instance.HideoutPlayer.PlayerBody.SlotViews.TryGetByKey(EquipmentSlot.Backpack, out var slotViewByItem) &&
				    slotViewByItem.ContainedItem?.Value != null &&
				    slotViewByItem.ParentedModel?.Value != null)
				{
					slotViewByItem.ParentedModel.Value.SetActive(!__0);
				}
				return false;
			}
		}
	}
}