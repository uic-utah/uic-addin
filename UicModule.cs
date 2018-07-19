using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProEvergreen;
using Reactive.Bindings;
using Serilog;
using uic_addin.Models;
using uic_addin.Services;
using uic_addin.Views;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace uic_addin {
    internal class UicModule : Module {
        public static readonly Dictionary<string, Layer> Layers = new Dictionary<string, Layer>(1);
        public static readonly Dictionary<string, DockPane> DockPanes = new Dictionary<string, DockPane>(2);
        public static readonly string FacilitySelectionState = "0";

        private static UicModule _this;
        private static SubscriptionToken _token;
        public static ReactiveProperty<Evergreen> Evergreen { get; private set; }
        public static ReactiveProperty<bool> IsCurrent { get; } = new ReactiveProperty<bool>();
        public static EvergreenSettings EvergreenSettings { get; set; }

        public static UicModule Current => _this ?? (_this =
                                               (UicModule)FrameworkApplication.FindModule("UICModule"));

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        private readonly IEnumerable<string> _addinKeys = new[] {"UICAddin.Evergreen.BetaChannel"};

        protected override bool Initialize() {
            Log.Debug("Initializing UIC Workflow Addin {version}", Assembly.GetExecutingAssembly().GetName().Version);
            Evergreen = new ReactiveProperty<Evergreen> {
                Value = new Evergreen("agrc", "uic-addin")
            };

            EvergreenSettings = new EvergreenSettings {
                BetaChannel = true
            };

            if (MapView.Active == null) {
                _token = MapViewInitializedEvent.Subscribe(args => CacheLayers(args.MapView));
            } else {
                CacheLayers(MapView.Active);

                if (Layers.Count < 1 || Layers.Any(x => x.Value == null)) {
                    return false;
                }
            }

            CachePanes();

            if (DockPanes.Count < 1 || DockPanes.Any(x => x.Value == null)) {
                return false;
            }

            return true;
        }

        protected override async Task OnReadSettingsAsync(ModuleSettingsReader settings) {
            Settings.Clear();

            if (settings == null) {
                await CheckForLastest();
                await base.OnReadSettingsAsync(null);

                return;
            }

            foreach (var key in _addinKeys) {
                var value = settings.Get(key);

                if (value != null) {
                    Settings[key] = value.ToString();
                }
            }

            EvergreenSettings.BetaChannel = Convert.ToBoolean(Settings["UICAddin.Evergreen.BetaChannel"]);

            await CheckForLastest();
        }

        protected override async Task OnWriteSettingsAsync(ModuleSettingsWriter settings) {
            foreach (var key in Settings.Keys) {
                settings.Add(key, Settings[key]);
            }

            await CheckForLastest();
        }

        protected override bool CanUnload() {
            foreach (var pane in DockPanes.Values) {
                if (pane.IsVisible) {
                    pane.Hide();
                }
            }

            return true;
        }

        public static void CacheLayers(MapView view = null) {
            Log.Debug("Caching project layers");

            if (_token != null) {
                MapViewInitializedEvent.Unsubscribe(_token);
            }

            var activeView = MapView.Active ?? view;

            if (activeView == null) {
                Log.Debug("MapView is empty");

                return;
            }

            Layers[FacilityModel.TableName] =
                LayerService.FindLayer(FacilityModel.TableName, activeView.Map) as FeatureLayer;
        }

        public static void CachePanes() {
            Log.Debug("Caching project dock panes");

            var paneIds = new[] {
                WorkflowDockPaneViewModel.DockPaneId
            };

            foreach (var id in paneIds) {
                FindPaneFromId(id);
            }
        }

        private static void FindPaneFromId(string id) {
            if (DockPanes.ContainsKey(id)) {
                return;
            }

            DockPanes[id] = FrameworkApplication.DockPaneManager.Find(id);
        }

        public static async Task CheckForLastest() {
            var useBetaChannel = true;
            if (Current.Settings.TryGetValue("UICAddin.Evergreen.BetaChannel", out var value)) {
                bool.TryParse(value, out useBetaChannel);
            }

            EvergreenSettings.LatestRelease = await Evergreen.Value.GetLatestReleaseFromGithub(useBetaChannel);
            var version = Evergreen.Value.GetCurrentAddInVersion();
            try {
                IsCurrent.Value = Evergreen.Value.IsCurrent(version.AddInVersion, EvergreenSettings.LatestRelease);
            } catch (ArgumentNullException) {
                if (version == null) {
                    // pro addin version couldnt be found
                    throw;
                }

                // github doesn't have a version. most likely only prereleases and no stable
                IsCurrent.Value = true;
            }
        }
    }
}
