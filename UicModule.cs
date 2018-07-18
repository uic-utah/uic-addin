using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Octokit;
using ProEvergreen;
using Serilog;
using uic_addin.Models;
using uic_addin.Services;
using uic_addin.Views;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;
using Notification = ArcGIS.Desktop.Framework.Notification;

namespace uic_addin {
    internal class UicModule : Module {
        public static Evergreen Evergreen { get; private set; }
        public static Release AddinRelease { get; set; }

        public static readonly Dictionary<string, Layer> Layers = new Dictionary<string, Layer>(1);
        public static readonly Dictionary<string, DockPane> DockPanes = new Dictionary<string, DockPane>(2);
        public static readonly string FacilitySelectionState = "0";
        public static UicModule Current => _this ?? (_this =
                                               (UicModule)FrameworkApplication.FindModule("UICModule"));

        private static UicModule _this;
        private static SubscriptionToken _token;

        protected override bool Initialize() {
            Log.Debug("Initializing UIC Workflow Addin {version}", Assembly.GetExecutingAssembly().GetName().Version);

            Evergreen = new Evergreen("agrc", "uic-addin"); 

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

        protected override bool CanUnload() {
            foreach (var pane in DockPanes.Values) {
                if (pane.IsVisible) {
                    pane.Hide();
                }
            }

            return true;
        }

        public static void CacheLayers(MapView view=null) {
            Log.Debug("Caching project layers");

            if (_token != null) {
                MapViewInitializedEvent.Unsubscribe(_token);
            }

            var activeView = MapView.Active ?? view;

            if (activeView == null) {
                Log.Debug("MapView is empty");

                return;
            }

            Layers[FacilityModel.TableName] = LayerService.FindLayer(FacilityModel.TableName, activeView.Map) as FeatureLayer;
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
    }
}
