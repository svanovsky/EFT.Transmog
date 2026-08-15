using System.Collections.Generic;
using System.Linq;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using IcyClawz.CustomInteractions;

namespace Transmog
{
	public class CustomInteractionsProvider : ICustomInteractionsProvider
	{
		internal static StaticIcons StaticIcons
		{
			get
			{
				return EFTHardSettings.Instance.StaticIcons;
			}
		}

		public enum SlotType
		{
			Armor,
			Vest,
			Backpack,
			Eyewear,
			FaceCover,
			Headwear,
			Earpiece,
			ArmBand,
			Count
		}

		private static readonly string[][] BASE_TEMPLATE_ID = {
			new[] { "5448e54d4bdc2dcc718b4568" },
			new[] { "5448e5284bdc2dcb718b4567" },
			new[] { "5448e53e4bdc2d60728b4567" },
			new[] { "5448e5724bdc2ddf718b4568" },
			new[] { "5a341c4686f77469e155819e" },
			new[] { "5a341c4086f77401f2541505" },
			new[] { "5645bcb74bdc2ded0b8b4578" },
			new[] { "5b3f15d486f77432d0509248", "6815465859b8c6ff13f94026" /*WTT - Pack 'n' Strap compatibility*/ }
		};

		public IEnumerable<CustomInteraction> GetCustomInteractions(ItemUiContext uiContext, EItemViewType viewType, Item item)
		{
			/*if (viewType != EItemViewType.Inventory)
			{
				yield break;
			}*/

			var available = BASE_TEMPLATE_ID.Select(s => s.Any(s2 => CheckTemplateParent(item.Template, s2))).ToArray();

			if (available.Any(b => b == true))
			{
                CustomInteraction customInteraction = new CustomInteraction(uiContext)
                {
                    Caption = () => "Transmog",
                    Icon = () => StaticIcons.GetAttributeIcon(EItemAttributeId.ArmorType),
                    SubMenu = () => CreateSubMenu(uiContext, item, available)
                };
                yield return customInteraction;
			}
		}

		private IEnumerable<CustomInteraction> CreateSubMenu(ItemUiContext uiContext, Item item, bool[] available)
		{
			for (var i = 0; i < available.Length; i++)
			{
				if (available[i])
				{
					var i1 = i;
					var showScav = Plugin.ShowScavInMenu.Value;
					var name = ((SlotType)i).ToString();
					var pmcCallname = showScav ? "PMC " : "";

					yield return new CustomInteraction(uiContext)
					{
						Caption = () => $"Set as {pmcCallname}{name}",
						Action = () =>
						{
							NotificationManager.DisplayMessageNotification(
								$"Set {item.ShortName} as Your PMC's visual {name.ToLowerInvariant()}.",
								ENotificationDurationType.Default, ENotificationIconType.Default, null);
							Plugin.PmcEquipments[i1].Set(item);
						}
					};
					if (showScav)
					{
						yield return new CustomInteraction(uiContext)
						{
							Caption = () => $"Set as Scav {name}",
							Action = () =>
							{
								NotificationManager.DisplayMessageNotification(
									$"Set {item.ShortName} as Your Scav's visual {name.ToLowerInvariant()}.",
									ENotificationDurationType.Default, ENotificationIconType.Default, null);
								Plugin.ScavEquipments[i1].Set(item);
							}
						};
					}
					yield return new CustomInteraction(uiContext)
					{
						Caption = () => $"Hide {pmcCallname}{name}",
						Action = () =>
						{
							NotificationManager.DisplayMessageNotification(
								$"Your PMC's {name.ToLowerInvariant()} will be visually hidden.",
								ENotificationDurationType.Default, ENotificationIconType.Default, null);
							Plugin.PmcEquipments[i1].Hide();
						}
					};
					if (showScav)
					{
						yield return new CustomInteraction(uiContext)
						{
							Caption = () => $"Hide Scav {name}",
							Action = () =>
							{
								NotificationManager.DisplayMessageNotification(
									$"Your Scav's {name.ToLowerInvariant()} will be visually hidden.",
									ENotificationDurationType.Default, ENotificationIconType.Default, null);
								Plugin.ScavEquipments[i1].Hide();
							}
						};
					}
					if (Plugin.PmcEquipments[i1].HasEffect())
					{
						yield return new CustomInteraction(uiContext)
						{
							Caption = () => $"Reset {pmcCallname}{name}",
							Action = () =>
							{
								NotificationManager.DisplayMessageNotification(
									$"Your PMC's {name.ToLowerInvariant()} will be shown normally.",
									ENotificationDurationType.Default, ENotificationIconType.Default, null);
								Plugin.PmcEquipments[i1].Reset();
							}
						};
					}
					if (showScav && Plugin.ScavEquipments[i1].HasEffect())
					{
						yield return new CustomInteraction(uiContext)
						{
							Caption = () => $"Reset Scav {name}",
							Action = () =>
							{
								NotificationManager.DisplayMessageNotification(
									$"Your Scav's {name.ToLowerInvariant()} will be shown normally.",
									ENotificationDurationType.Default, ENotificationIconType.Default, null);
								Plugin.ScavEquipments[i1].Reset();
							}
						};
					}
				}
			}
		}

		private static bool CheckTemplateParent(ItemTemplate template, string id)
		{
			if (template._id == id)
				return true;
			if (template.Parent != null)
				return CheckTemplateParent(template.Parent, id);
			return false;
		}
	}
}