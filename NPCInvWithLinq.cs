﻿using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ItemFilterLibrary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NPCInvWithLinq
{
    public class NPCInvWithLinq : BaseSettingsPlugin<NPCInvWithLinqSettings>
    {
        private readonly TimeCache<List<ItemData>> _itemLabels;
        private ItemFilter _itemFilter;
        private PurchaseWindow _purchaseWindowHideout;
        private PurchaseWindow _purchaseWindow;
        public NPCInvWithLinq()
        {
            Name = "NPCInvWithLinq";
            _itemLabels = new TimeCache<List<ItemData>>(UpdateCurrentTradeWindow, 50);
        }
        public override bool Initialise()
        {
            Settings.FilterFile.OnValueSelected = _ => LoadRuleFiles();
            Settings.ReloadFilters.OnPressed = LoadRuleFiles;
            LoadRuleFiles();
            return true;
        }

        public override Job Tick()
        {
            return null;
        }

        public override void Render()
        {
            _purchaseWindowHideout = GameController.Game.IngameState.IngameUi.PurchaseWindowHideout;
            _purchaseWindow = GameController.Game.IngameState.IngameUi.PurchaseWindow;

            if (!_purchaseWindowHideout.IsVisible && !_purchaseWindow.IsVisible)
                return;

            foreach (var item in _itemLabels.Value)
            {
                if (item == null) continue;
                if (!ItemInFilter(item)) continue;

                Graphics.DrawFrame(item.ClientRectangleCache, Settings.FrameColor, Settings.FrameThickness);
            }

            if (Settings.FilterTest.Value is { Length: > 0 } &&
                GameController.IngameState.UIHover is { Address: not 0 } h &&
                h.Entity.IsValid)
            {
                var f = ItemFilter.LoadFromString(Settings.FilterTest);
                var matched = f.Matches(new ItemData(h.Entity, GameController.Files));
                DebugWindow.LogMsg($"Debug item match on hover: {matched}");
            }
        }

        private void LoadRuleFiles()
        {
            var pickitConfigFileDirectory = Path.Combine(ConfigDirectory);

            if (!Directory.Exists(pickitConfigFileDirectory))
            {
                Directory.CreateDirectory(pickitConfigFileDirectory);
                return;
            }

            var dirInfo = new DirectoryInfo(pickitConfigFileDirectory);
            Settings.FilterFile.Values = dirInfo.GetFiles("*.ifl").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            if (Settings.FilterFile.Values.Any() && !Settings.FilterFile.Values.Contains(Settings.FilterFile.Value))
            {
                Settings.FilterFile.Value = Settings.FilterFile.Values.First();
            }

            if (!string.IsNullOrWhiteSpace(Settings.FilterFile.Value))
            {
                var filterFilePath = Path.Combine(pickitConfigFileDirectory, $"{Settings.FilterFile.Value}.ifl");
                if (File.Exists(filterFilePath))
                {
                    _itemFilter = ItemFilter.LoadFromPath(filterFilePath);
                }
                else
                {
                    _itemFilter = null;
                    LogError("Item filter file not found, plugin will not work");
                }
            }
        }

        private List<ItemData> UpdateCurrentTradeWindow()
        {
            if (_purchaseWindowHideout == null || _purchaseWindow == null)
                return new List<ItemData>();

            PurchaseWindow purchaseWindowItems = null;
            WorldArea currentWorldArea = GameController.Game.IngameState.Data.CurrentWorldArea;

            if (currentWorldArea.IsHideout && _purchaseWindowHideout.IsVisible)
                purchaseWindowItems = _purchaseWindowHideout;
            else if (currentWorldArea.IsTown && _purchaseWindow.IsVisible)
                purchaseWindowItems = _purchaseWindow;

            if (purchaseWindowItems == null)
                return new List<ItemData>();

            IList<NormalInventoryItem> VendorContainer = purchaseWindowItems?.TabContainer?.VisibleStash?.VisibleInventoryItems;


            var labels = purchaseWindowItems.TabContainer;

            return VendorContainer.ToList().Where(x => x.IsVisible && x.Item?.Path != null)
                .Select(x => new ItemData(x.Item, GameController.Files, x.GetClientRectCache))
                .ToList();
        }
        private bool ItemInFilter(ItemData item)
        {
            return (_itemFilter?.Matches(item, true) ?? false);
        }
    }
}