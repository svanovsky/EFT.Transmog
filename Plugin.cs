using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPT.Reflection.Utils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using IcyClawz.CustomInteractions;
using static Transmog.CustomInteractionsProvider;

namespace Transmog
{
	[BepInPlugin("net.hakugyokurou.Transmog", "Transmog", "1.3.1")]
	[BepInDependency("com.IcyClawz.CustomInteractions")]
	public class Plugin : BaseUnityPlugin
	{
		private static ManualLogSource logger;

		public static ConfigEntry<bool> ShowScavInMenu;
		public static ConfigEntry<bool> DisableScavTransmogInLobby;
		public static ConfigEntry<bool> AffectIngameModel;
		public static CostumeItem[] PmcEquipments = new CostumeItem[(int) SlotType.Count];
		public static CostumeItem[] ScavEquipments = new CostumeItem[(int) SlotType.Count];

		public static string PmcId { get; private set; } = "";
		public static string ScavId { get; private set; } = "";
		public static readonly Dictionary<string, CostumeItem[]> costumes = new Dictionary<string, CostumeItem[]>();

		private void Awake()
		{
			logger = Logger;
			logger.LogInfo("Loading: Transmog - V1.3.1");

			ShowScavInMenu = Config.Bind("Generals", "Show Scav in transmog menu", false, "Allow you to apply transmog to your scavs.");
			DisableScavTransmogInLobby = Config.Bind("Generals", "Show original equipment for Scav in the lobby", false, 
				"Disable transmog for Scavs in the matchmaking screen so you can see their equipment.");
			AffectIngameModel = Config.Bind("Generals", "Affect ingame player entity", true, 
				"When enabled, your player models in raid (such as shadow, third person model when you have Freecam mod) will also be affected by Transmog. " +
				"If disabled, only the player models in the tab menu and main menu will be affected. " +
				"If you toggle this option in a raid, it will not take effect until the next raid.");

			PmcEquipments[(int)SlotType.Armor] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Armor", ""));
			PmcEquipments[(int)SlotType.Vest] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Vesc", ""));
			PmcEquipments[(int)SlotType.Backpack] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Backpack", ""));
			PmcEquipments[(int)SlotType.Eyewear] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Eyewear", ""));
			PmcEquipments[(int)SlotType.FaceCover] = new CostumeItemLocal(Config.Bind("PMC Transmog", "FaceCover", ""));
			PmcEquipments[(int)SlotType.Headwear] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Headwear", ""));
			PmcEquipments[(int)SlotType.Earpiece] = new CostumeItemLocal(Config.Bind("PMC Transmog", "Earpiece", ""));
			PmcEquipments[(int)SlotType.ArmBand] = new CostumeItemLocal(Config.Bind("PMC Transmog", "ArmBand", ""));

			ScavEquipments[(int)SlotType.Armor] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Armor", ""));
			ScavEquipments[(int)SlotType.Vest] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Vesc", ""));
			ScavEquipments[(int)SlotType.Backpack] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Backpack", ""));
			ScavEquipments[(int)SlotType.Eyewear] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Eyewear", ""));
			ScavEquipments[(int)SlotType.FaceCover] = new CostumeItemLocal(Config.Bind("Scav Transmog", "FaceCover", ""));
			ScavEquipments[(int)SlotType.Headwear] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Headwear", ""));
			ScavEquipments[(int)SlotType.Earpiece] = new CostumeItemLocal(Config.Bind("Scav Transmog", "Earpiece", ""));
			ScavEquipments[(int)SlotType.ArmBand] = new CostumeItemLocal(Config.Bind("Scav Transmog", "ArmBand", ""));

			new PlayerModelViewPatch().Enable();
			new PlayerIconImagePatch().Enable();
			new TacticalClothingViewPatch().Enable();
			new PlayerBodyPatch().Enable();
			new MatchMakerSideSelectionScreenPatch().Enable();
			CustomInteractionsManager.Register(new CustomInteractionsProvider());
		}

		public static void LogMessage(string message)
		{
			logger.LogMessage(message);
		}

		public static void LogInfo(string message)
		{
			logger.LogInfo(message);
		}

		public static void LogDebug(string message)
		{
			logger.LogDebug(message);
		}

		public static void LogError(string message)
		{
			logger.LogError(message);
		}

		public static Profile GetPlayerPmcProfile()
		{
			Profile profile = ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile;
			if (profile == null) return null; // It returns null while selecting side
			if (profile.Id == PmcId) return profile;
			costumes.Remove(PmcId);
			PmcId = profile.Id;
			costumes.Add(PmcId, PmcEquipments);

			return profile;
		}

		public static Profile GetPlayerScavProfile()
		{
			Profile profile = ClientAppUtils.GetMainApp().GetClientBackEndSession().ProfileOfPet;
			if (profile.Id == ScavId) return profile;
			costumes.Remove(ScavId);
			ScavId = profile.Id;
			costumes.Add(ScavId, ScavEquipments);

			return profile;
		}

		public static bool ToSlotType(EquipmentSlot equipmentSlot, out SlotType slotType)
		{
			switch (equipmentSlot)
			{
				case EquipmentSlot.Backpack: slotType = SlotType.Backpack; break;
				case EquipmentSlot.TacticalVest: slotType = SlotType.Vest; break;
				case EquipmentSlot.ArmorVest: slotType = SlotType.Armor; break;
				case EquipmentSlot.Eyewear: slotType = SlotType.Eyewear; break;
				case EquipmentSlot.FaceCover: slotType = SlotType.FaceCover; break;
				case EquipmentSlot.Headwear: slotType = SlotType.Headwear; break;
				case EquipmentSlot.Earpiece: slotType = SlotType.Earpiece; break;
				case EquipmentSlot.ArmBand: slotType = SlotType.ArmBand; break;
				default:
					slotType = SlotType.Armor;
					return false;
			}
			return true;
		}

		public static InventoryEquipment CloneAndModifyEquipmentClass(InventoryEquipment originEquipmentClass, bool isScav = false)
		{
			// GClass3380.smethod_2<T>(item, cloner, bool, bool) is gone in SPT 4.1; the clone utility is now
			// a plain extension method that only needs the item itself.
			var newEquipmentClass = originEquipmentClass.CloneVisibleItem();

			var newEquipments = isScav ? ScavEquipments : PmcEquipments;
			for (int i = 0; i < (int) SlotType.Count; i++)
			{
				if (newEquipments[i].HasEffect())
				{
					EquipmentSlot slotIndex;
					switch ((SlotType) i)
					{
						case SlotType.Armor: slotIndex = EquipmentSlot.ArmorVest; break;
						case SlotType.Vest: slotIndex = EquipmentSlot.TacticalVest; break;
						case SlotType.Backpack: slotIndex = EquipmentSlot.Backpack; break;
						case SlotType.Eyewear: slotIndex = EquipmentSlot.Eyewear; break;
						case SlotType.FaceCover: slotIndex = EquipmentSlot.FaceCover; break;
						case SlotType.Headwear: slotIndex = EquipmentSlot.Headwear; break;
						case SlotType.Earpiece: slotIndex = EquipmentSlot.Earpiece; break;
						case SlotType.ArmBand: slotIndex = EquipmentSlot.ArmBand; break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					LogDebug($"Apply transmog to {slotIndex} slot.");
					newEquipments[i].AffectSlot(newEquipmentClass.GetSlot(slotIndex));
				}
			}

			return newEquipmentClass;
		}

		public abstract class CostumeItem
		{
			protected bool _ready = false;
			protected bool _hide = false;
			protected Item _item;

			public abstract string Value { get; set; }

			public event Action<CostumeItem, string, string> ValueChanged;

			public Item Get()
			{
				if (!_ready)
				{
					_ready = true;
					_item = Deserialize(Value);
				}
				return _item;
			}

			public void Set(Item item)
			{
				_hide = false;
				_ready = true;
				Value = Serialize(item);
				_item = Deserialize(Value);
			}

			public void Hide()
			{
				_hide = true;
				_item = null;
				_ready = true;
				Value = "hide";
			}

			public void Reset()
			{
				_hide = false;
				_item = null;
				_ready = true;
				Value = string.Empty;
			}

			public bool IsHide()
			{
				if (!_ready)
				{
					_ready = true;
					_item = Deserialize(Value);
				}
				return _hide;
			}

			public bool HasEffect()
			{
				return IsHide() || Get() != null;
			}

			public void AffectSlot(Slot slot)
			{
				var hide = IsHide();
				var item = Get();
				if (hide || item != null)
					slot.RemoveItem();
				if (item != null)
				{
					item.CurrentAddress = null;
					slot.AddWithoutRestrictions(item);
					slot.ApplyContainedItem();
				}
			}

			public Item Deserialize(string config)
			{
				_hide = false;
				if (string.IsNullOrWhiteSpace(config))
					return null;
				// ItemFactoryClass was renamed to ItemFactory in SPT 4.1.
				var itemFactory = Singleton<ItemFactory>.Instance;
				var solver = itemFactory.ItemTemplates;
				var queue = new Queue<Item>();
				var splittedConfig = config.Split(',');
				if (splittedConfig.Length > 0 &&
				    splittedConfig[0].Equals("hide", StringComparison.InvariantCultureIgnoreCase))
				{
					_hide = true;
					return null;
				}
				foreach (var s in splittedConfig)
				{
					var id = s.Trim().ToLowerInvariant();
					// MongoID now has implicit conversions to/from string, so TryGetValue and CreateItem's
					// (string, string, UnparsedData) overload both accept the plain string id unchanged.
					if (id == "null" || !solver.TryGetValue(id, out var itemTemplate))
						queue.Enqueue(null);
					else
					{
						queue.Enqueue(itemFactory.CreateItem(MongoID.Generate(false), id, null));
					}
				}

				// Queue<T>.TryDequeue isn't available on net472 (pre-dates this project's target framework,
				// unrelated to the SPT 4.1 API changes).
				if (queue.Count == 0)
					return null;
				var item = queue.Dequeue();
				if (item == null)
					return null;

				LogInfo("Begin deserialize...");
				LogInfo($"Append {item.TemplateId}");

				void DeserializeDo(Item _rootItem, Queue<Item> _itemQueue)
				{
					if (_rootItem is CompoundItem loot)
					{
						var slots = loot.Slots;
						foreach (var slot in slots)
						{
							// Slot.CanAccept no longer exists in SPT 4.1; CheckCompatibility is the closest
							// remaining candidate (validates the item against the slot's filters) but this
							// specific substitution is unverified against runtime behavior -- watch here first
							// if transmogged compound items (e.g. rigs with attached pouches) stop nesting correctly.
							var element = _itemQueue.Count > 0 ? _itemQueue.Dequeue() : null;
							if (element != null &&
							    slot.CheckCompatibility(element))
							{
								slot.AddWithoutRestrictions(element);
								LogInfo($"Append {element.TemplateId}");
							}
						}
						foreach (var slot in slots)
						{
							if (slot.ContainedItem != null)
							{
								DeserializeDo(slot.ContainedItem, _itemQueue);
							}
						}
					}
				}

				DeserializeDo(item, queue);
				return item;
			}

			public string Serialize(Item item)
			{
				if (item == null)
					return string.Empty;
				var sb = new StringBuilder();

				LogInfo("Begin serialize...");
				LogInfo($"Append {item.TemplateId}");
				sb.Append(item.TemplateId);
				void SerializeDo(Item _item2, StringBuilder _sb)
				{
					if (_item2 is CompoundItem loot)
					{
						var slots = loot.Slots;
						foreach (var slot in slots)
						{
							if (slot.ContainedItem != null)
							{
								LogInfo($"Append {slot.ContainedItem.TemplateId}");
								_sb.Append(", ").Append(slot.ContainedItem.TemplateId);
							}
							else
							{
								_sb.Append(", null");
							}
						}
						foreach (var slot in slots)
						{
							if (slot.ContainedItem != null)
							{
								SerializeDo(slot.ContainedItem, _sb);
							}
						}
					}
				}

				SerializeDo(item, sb);
				return sb.ToString();
			}

			protected void OnValueChanged(string oldValue, string newValue)
			{
				ValueChanged?.Invoke(this, oldValue, newValue);
			}
		}

		public class CostumeItemLocal : CostumeItem
		{
			private readonly ConfigEntry<string> _configEntry;

			private string lastValue;

			public override string Value
			{
				get => _configEntry.Value;
				set
				{
					if (value == _configEntry.Value) return;
					_configEntry.Value = value;
					OnValueChanged(lastValue, _configEntry.Value);
					lastValue = value;
				}
			}

			public CostumeItemLocal(ConfigEntry<string> configEntry)
			{
				_configEntry = configEntry;
				lastValue = _configEntry.Value;
				_configEntry.SettingChanged += ConfigEntryOnSettingChanged;
			}

			private void ConfigEntryOnSettingChanged(object sender, EventArgs e)
			{
				if (_configEntry.Value == lastValue) return;

				_item = Deserialize(_configEntry.Value);
				_ready = true;
				OnValueChanged(lastValue, _configEntry.Value);
				lastValue = _configEntry.Value;
			}
		}

		public class CostumeItemRemote : CostumeItem
		{
			private string itemValue;
			
			public sealed override string Value
			{
				get => itemValue;
				set
				{
					if (value == itemValue) return;
					string oldValue = itemValue;
					itemValue = value;
					_item = Deserialize(itemValue);
					_ready = true;
					OnValueChanged(oldValue, itemValue);
				}
			}

			public CostumeItemRemote(string value)
			{
				itemValue = value;
			}
		}
	}
}
