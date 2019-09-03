using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Extensions;
using uic_addin.Models;
using uic_addin.Services;

namespace uic_addin.Views {
    internal class WorkflowViewModel : TOCMapPaneProviderPane {
        private const string ViewPaneId = "WorkflowPane";
        private readonly SubscriptionToken _subscriptionToken;
        private SubscriptionToken _mapLoadToken;

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public WorkflowViewModel(CIMView view) : base(view) {
            _dockUnderMapView = true;

            Prompt = LayerAdded.ObserveAddChanged().Select(x => {
                PromptForProjection(MapView.Active?.Map);

                return true;
            }).ToReactiveProperty();

            LayersAddedEvent.Subscribe(args => LayerAdded.AddOnScheduler(args.Layers));

            UpdateToVersionMessage = UicModule.Current.IsCurrent.Select(x => {
                                                  if (x) {
                                                      return "Your add-in is up to date! 💙";
                                                  }

                                                  return
                                                      $"Update to {UicModule.Current.EvergreenSettings?.LatestRelease?.TagName}";
                                              })
                                              .ToReactiveProperty();

            UpdateSelf.Subscribe(async () => await UpdateAddin());

            ShowUpdate = UicModule.Current.IsCurrent.Select(x => !x)
                                  .ToReactiveProperty();
        }

        // todo explore PropertySheet.Show
        public ICommand ShowSettings => FrameworkApplication.GetPlugInWrapper("esri_core_showOptionsSheetButton") as
            ICommand;

        public ReactiveProperty<bool> ShowUpdate { get; set; }

        public ReactiveProperty<string> UpdateToVersionMessage { get; set; }

        public ReactiveProperty<bool> Prompt { get; set; }

        public ReactiveProperty<bool> IsReady { get; set; } = new ReactiveProperty<bool>(false);

        public ReactiveCollection<IEnumerable<Layer>> LayerAdded { get; set; } =
            new ReactiveCollection<IEnumerable<Layer>>();

        public ReactiveCommand UpdateSelf { get; set; } = new ReactiveCommand();

        /// <summary>
        ///     Must be overridden in child classes used to persist the state of the view to the CIM.
        /// </summary>
        /// <remarks>View state is called on each project save</remarks>
        public override CIMView ViewState {
            get {
                _cimView.InstanceID = (int)InstanceID;
                //Cache content in _cimView.ViewProperties or in _cimView.ViewXML
                //_cimView.ViewXML = new XDocument(new XElement("Root",
                //new XElement("custom", "custom value"))).ToString(SaveOptions.DisableFormatting);
                return _cimView;
            }
        }

        private static async Task UpdateAddin() {
            if (UicModule.Current.EvergreenSettings.LatestRelease == null) {
                return;
            }

            await UicModule.Current.Evergreen.Value.Update(UicModule.Current.EvergreenSettings.LatestRelease);

            var result =
                MessageBox.Show("A restart is required to complete the update. Would you like to exit Pro now?",
                                "Evergreen: Restart Required",
                                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes) {
                await FrameworkApplication.ShutdownAsync();
            }
        }

        /// <summary>
        ///     Create a new instance of the pane.
        /// </summary>
        internal static WorkflowViewModel Create(MapView mapView) {
            var view = new CIMGenericView {
                ViewType = ViewPaneId,
                ViewProperties = new Dictionary<string, object>()
            };

            if (mapView == null) {
                return null;
            }

            view.ViewProperties["MAPURI"] = mapView.Map.URI;

            if (!(FrameworkApplication.Panes.Create(ViewPaneId, view) is WorkflowViewModel newPane)) {
                return null;
            }

            newPane.Caption = "UIC Workflow";

            return newPane;
        }

        private static void PromptForProjection(ILayerContainer map) => ThreadService.RunOnBackground(() => {
            if (map == null && MapView.Active?.Map == null) {
                return;
            }

            var activeMap = map ?? MapView.Active.Map;

            var layers = Enumerable.Empty<Layer>();

            layers = activeMap.Layers.Where(layer => layer.GetSpatialReference().Wkid != 26912);

            if (!layers.Any()) {
                return;
            }

            var problems = layers.Select(x => new {
                x.Name,
                Sr = x.GetSpatialReference().Name
            });

            var message =
                problems.Aggregate("",
                                   (current, item) => current +
                                                      $"Layer {item.Name} has a spatial reference of {item.Sr}{Environment.NewLine}");
            message += $"{Environment.NewLine}Please reproject these layers";

            MessageBox.Show(message, "Spatial Reference Issue");
        });

        /// <summary>
        ///     Called when the pane is initialized.
        /// </summary>
        protected override async Task InitializeAsync() {
            var uri = ((CIMGenericView)_cimView).ViewProperties["MAPURI"] as string;
            await SetMapURI(uri);

            await base.InitializeAsync();

            FrameworkApplication.State.Deactivate(UicModule.Current.WorkflowModelState);
            _mapLoadToken = MapViewInitializedEvent.Subscribe(args => PromptForProjection(args.MapView.Map));
        }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            MapSelectionChangedEvent.Unsubscribe(_subscriptionToken);
            MapViewInitializedEvent.Unsubscribe(_mapLoadToken);

            ShowUpdate.Dispose();
            UpdateToVersionMessage.Dispose();
            UpdateSelf.Dispose();

            FrameworkApplication.State.Activate(UicModule.Current.WorkflowModelState);

            await base.UninitializeAsync();
        }
    }
}
