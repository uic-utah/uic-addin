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
        private static UicModule _this;
        private readonly IEnumerable<string> _addinKeys = new[] {"UICAddin.Evergreen.BetaChannel"};
        public readonly Dictionary<string, DockPane> DockPanes = new Dictionary<string, DockPane>(2);
        public readonly string FacilitySelectionState = "0";
        public readonly string WorkflowModelState = "workflow_pane_enabled";
        public readonly Dictionary<string, Layer> Layers = new Dictionary<string, Layer>(1);
        private SubscriptionToken _token;

        public ReactiveProperty<Evergreen> Evergreen { get; } = new ReactiveProperty<Evergreen> {
            Value = new Evergreen("agrc", "uic-addin")
        };

        public ReactiveProperty<bool> IsCurrent { get; } = new ReactiveProperty<bool>(true);

        public EvergreenSettings EvergreenSettings { get; set; } = new EvergreenSettings {
            BetaChannel = true
        };

        public static UicModule Current => _this ?? (_this =
                                               (UicModule)FrameworkApplication.FindModule("UICModule"));

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        protected override bool Initialize() {
            Log.Debug("Initializing UIC Workflow Addin {version}", Assembly.GetExecutingAssembly().GetName().Version);

            if (MapView.Active == null) {
                _token = MapViewInitializedEvent.Subscribe(args => CacheLayers(args.MapView));
            } else {
                CacheLayers(MapView.Active);

                if (Layers.Count < 1 || Layers.Any(x => x.Value == null)) {
                    return false;
                }
            }

            FrameworkApplication.State.Activate(WorkflowModelState);

            return true;
        }

        protected override async Task OnReadSettingsAsync(ModuleSettingsReader settings) {
            Settings.Clear();

            if (settings == null) {
                try {
                    await CheckForLastest();
                } catch {
                    // ignored
                }

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

            try {
                await CheckForLastest();
            } catch {
                // ignored
            }
        }

        protected override async Task OnWriteSettingsAsync(ModuleSettingsWriter settings) {
            foreach (var key in Settings.Keys) {
                settings.Add(key, Settings[key]);
            }

            try {
                await CheckForLastest();
            } catch {
                // ignored
            }
        }

        protected override bool CanUnload() {
            foreach (var pane in DockPanes.Values) {
                if (pane.IsVisible) {
                    pane.Hide();
                }
            }

            return true;
        }

        public void CacheLayers(MapView view = null) {
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

        public void CachePanes() {
            Log.Debug("Caching project dock panes");

            
        }

        private void FindPaneFromId(string id) {
            if (DockPanes.ContainsKey(id)) {
                return;
            }

            DockPanes[id] = FrameworkApplication.DockPaneManager.Find(id);
        }

        public async Task CheckForLastest() {
            var useBetaChannel = true;
            if (Current.Settings.TryGetValue("UICAddin.Evergreen.BetaChannel", out var value)) {
                bool.TryParse(value, out useBetaChannel);
            }

            EvergreenSettings.LatestRelease = await Evergreen.Value.GetLatestReleaseFromGithub(useBetaChannel);
            EvergreenSettings.CurrentVersion = Evergreen.Value.GetCurrentAddInVersion();
            try {
                IsCurrent.Value = Evergreen.Value.IsCurrent(EvergreenSettings.CurrentVersion.AddInVersion,
                                                            EvergreenSettings.LatestRelease);
            } catch (ArgumentNullException) {
                if (EvergreenSettings.CurrentVersion == null) {
                    // pro addin version couldnt be found
                    throw;
                }

                // github doesn't have a version. most likely only prereleases and no stable
                IsCurrent.Value = true;
            }
        }
    }
}
