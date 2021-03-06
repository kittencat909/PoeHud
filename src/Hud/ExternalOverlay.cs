﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.AdvancedTooltip;
using PoeHUD.Hud.Dps;
using PoeHUD.Hud.Health;
using PoeHUD.Hud.Icons;
using PoeHUD.Hud.Interfaces;
using PoeHUD.Hud.InventoryPreview;
using PoeHUD.Hud.KC;
using PoeHUD.Hud.Loot;
using PoeHUD.Hud.Menu;
using PoeHUD.Hud.Preload;
using PoeHUD.Hud.Settings;
using PoeHUD.Hud.Trackers;
using PoeHUD.Hud.XpRate;
using PoeHUD.Poe.UI;

using SharpDX;
using SharpDX.Windows;

using Color = System.Drawing.Color;
using Graphics2D = PoeHUD.Hud.UI.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace PoeHUD.Hud
{
    internal class ExternalOverlay : RenderForm
    {
        private readonly SettingsHub settings;

        private readonly GameController gameController;

        private readonly Func<bool> gameEnded;

        private readonly IntPtr gameHandle;

        private readonly List<IPlugin> plugins = new List<IPlugin>();

        private Graphics2D graphics;

        public ExternalOverlay(GameController gameController, Func<bool> gameEnded)
        {
            settings = SettingsHub.Load();

            this.gameController = gameController;
            this.gameEnded = gameEnded;
            gameHandle = gameController.Window.Process.MainWindowHandle;

            SuspendLayout();
            Text = MathHepler.GetRandomWord(MathHepler.Randomizer.Next(7) + 5);
            TransparencyKey = Color.Transparent;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            ShowIcon = false;
            //ShowInTaskbar = false;
            TopMost = true;
            ResumeLayout(false);
            Load += OnLoad;
        }

        private async void CheckGameState()
        {
            while (!gameEnded())
            {
                await Task.Delay(500);
            }
            graphics.Dispose();
            Close();
        }

        private async void CheckGameWindow()
        {
            while (!gameEnded())
            {
                await Task.Delay(1000);
                Rectangle gameSize = WinApi.GetClientRectangle(gameHandle);
                Bounds = gameSize;
            }
        }

        private IEnumerable<MapIcon> GatherMapIcons()
        {
            IEnumerable<IPluginWithMapIcons> pluginsWithIcons = plugins.OfType<IPluginWithMapIcons>();
            return pluginsWithIcons.SelectMany(iconSource => iconSource.GetIcons());
        }

        private Vector2 GetLeftCornerMap()
        {
            RectangleF clientRect = gameController.Game.IngameState.IngameUi.Map.SmallMinimap.GetClientRect();
            return new Vector2(clientRect.X - 10, clientRect.Y + 5);
        }

        private Vector2 GetUnderCornerMap()
        {
            const int EPSILON = 1;
            Element gemPanel = gameController.Game.IngameState.IngameUi.GemLvlUpPanel;
            RectangleF gemPanelRect = gemPanel.GetClientRect();
            RectangleF mapRect = gameController.Game.IngameState.IngameUi.Map.SmallMinimap.GetClientRect();
            RectangleF clientRect = gemPanel.IsVisible && Math.Abs(gemPanelRect.Right - mapRect.Right) < EPSILON
                ? gemPanel.GetClientRect()
                : mapRect;
            return new Vector2(mapRect.X + mapRect.Width, clientRect.Y + clientRect.Height + 10);
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            SettingsHub.Save(settings);
            plugins.ForEach(plugin => plugin.Dispose());
            graphics.Dispose();
        }

        private void OnDeactivate(object sender, EventArgs e)
        {
            BringToFront();
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            Bounds = WinApi.GetClientRectangle(gameHandle);
            WinApi.EnableTransparent(Handle, Bounds);
            graphics = new Graphics2D(this, Bounds.Width, Bounds.Height);
            graphics.Render += OnRender;

            plugins.Add(new HealthBarPlugin(gameController, graphics, settings.HealthBarSettings));
            plugins.Add(new MinimapPlugin(gameController, graphics, GatherMapIcons, settings.MapIconsSettings));
            plugins.Add(new LargeMapPlugin(gameController, graphics, GatherMapIcons, settings.MapIconsSettings));
            plugins.Add(new MonsterTracker(gameController, graphics, settings.MonsterTrackerSettings));
            plugins.Add(new PoiTracker(gameController, graphics, settings.PoiTrackerSettings));

            var leftPanel = new PluginPanel(GetLeftCornerMap);
            leftPanel.AddChildren(new XpRatePlugin(gameController, graphics, settings.XpRateSettings));
            leftPanel.AddChildren(new PreloadAlertPlugin(gameController, graphics, settings.PreloadAlertSettings));
            var horizontalPanel = new PluginPanel(Direction.Left);
            horizontalPanel.AddChildren(new DpsMeterPlugin(gameController, graphics, settings.DpsMeterSettings));
            horizontalPanel.AddChildren(new KillsCounterPlugin(gameController, graphics, settings.KillsCounterSettings));
            leftPanel.AddChildren(horizontalPanel);
            plugins.AddRange(leftPanel.GetPlugins());

            var underPanel = new PluginPanel(GetUnderCornerMap);
            underPanel.AddChildren(new ItemAlertPlugin(gameController, graphics, settings.ItemAlertSettings));
            plugins.AddRange(underPanel.GetPlugins());

            plugins.Add(new AdvancedTooltipPlugin(gameController, graphics, settings.AdvancedTooltipSettings));
            plugins.Add(new InventoryPreviewPlugin(gameController, graphics, settings.InventoryPreviewSettings));
            plugins.Add(new MenuPlugin(gameController, graphics, settings));

            Deactivate += OnDeactivate;
            FormClosing += OnClosing;

            CheckGameWindow();
            CheckGameState();
            await Task.Run(() => graphics.RenderLoop());
        }

        private void OnRender()
        {
            if (gameController.InGame && WinApi.IsForegroundWindow(gameHandle)
                && !gameController.Game.IngameState.IngameUi.TreePanel.IsVisible)
            {
                gameController.RefreshState();
                plugins.ForEach(x => x.Render());
            }
        }
    }
}