using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public WorkflowViewModel(CIMView view) : base(view) {
            _dockUnderMapView = true;

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

            UseSelection = new RelayCommand(async () => {
                var facilityLayer = UicModule.Current.Layers[FacilityModel.TableName];
                var ids = await LayerService.GetSelectedIdsFor(facilityLayer);

                var selectedIds = await QueryService.GetFacilityIdsFor(ids, facilityLayer.Map);
                if (selectedIds.Count() > 1) {
                    FrameworkApplication.AddNotification(new Notification {
                        Message = $"Selected {selectedIds.Count()} facilities. Showing {selectedIds.FirstOrDefault()}"
                    });
                }

                Facilities.Value = new[] {selectedIds.FirstOrDefault()};
            }, () => MapView.Active?.Map.SelectionCount > 0);

            ZoomToFacility = new RelayCommand(async () => {
                var facilityLayer = UicModule.Current.Layers[FacilityModel.TableName] as BasicFeatureLayer;

                await MapView.Active.ZoomToAsync(facilityLayer, Model.Value.ObjectId, TimeSpan.FromMilliseconds(250));
            }, () => {
                if (MapView.Active == null) {
                    return false;
                }

                return Model?.Value?.ObjectId > 0;
            });

            ShowAttributeEditorForSelectedRecord = new RelayCommand(() => {
                var edits = FrameworkApplication.DockPaneManager.Find("esri_editing_AttributesDockPane");
                edits.Activate(true);
            }, () => MapView.Active?.Map.SelectionCount > 0);

            Facilities = FacilityId.Select(async id => {
                                       if (id.Length < 4) {
                                           return Enumerable.Empty<string>();
                                       }

                                       return await QueryService.GetFacilityIdsFor(id, MapView.Active.Map);
                                   })
                                   .CatchIgnore()
                                   .Switch()
                                   .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);

            Facilities.ObserveOn(Application.Current.Dispatcher)
                      .ForEachAsync(async items => {
                          if (items?.Count() != 1) {
                              Model.Value = new FacilityModel();

                              return;
                          }

                          Model.Value = await QueryService.GetFacilityFor(items.First());

                          var selection = await ThreadService.RunOnBackground(MapView.Active.Map.GetSelection);

                          if (selection.Count(x => x.Key.Name == FacilityModel.TableName) != 0) {
                              return;
                          }

                          var facilityLayer = UicModule.Current.Layers[FacilityModel.TableName];
                          await LayerService.SetSelectionFromId(Model.Value.ObjectId, facilityLayer);
                      });

            _subscriptionToken = MapSelectionChangedEvent.Subscribe(async args => {
                if (!FrameworkApplication.State.Contains(UicModule.Current.FacilitySelectionState)) {
                    return;
                }

                var facilitySelection = args.Selection?.Where(x => string.Equals(x.Key.Name.SplitAndTakeLast('.'),
                                                                                 "uicfacility",
                                                                                 StringComparison
                                                                                     .InvariantCultureIgnoreCase));

                var facilityObjectIds = facilitySelection?.SelectMany(x => x.Value);

                if (!facilityObjectIds.Any()) {
                    Facilities.Value = Enumerable.Empty<string>();

                    return;
                }

                var facilityIds = await QueryService.GetFacilityIdsFor(facilityObjectIds, args.Map);
                if (facilityIds.Count() > 1) {
                    FrameworkApplication.AddNotification(new Notification {
                        Message = $"Selected {facilityIds.Count()} facilities. Showing {facilityIds.FirstOrDefault()}"
                    });
                }

                Facilities.Value = new[] {facilityIds.FirstOrDefault()};
            });
        }

        public RelayCommand UseSelection { get; }

        public RelayCommand ZoomToFacility { get; }

        public RelayCommand ShowAttributeEditorForSelectedRecord { get; }

        public ICommand ShowSettings => FrameworkApplication.GetPlugInWrapper("esri_core_showOptionsSheetButton") as
            ICommand;

        // Exposed Model
        public ReactiveProperty<FacilityModel> Model { get; set; } =
            new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);

        public ReactiveProperty<string> FacilityId { get; set; } =
            new ReactiveProperty<string>(mode: ReactivePropertyMode.DistinctUntilChanged);

        public ReactiveProperty<IEnumerable<string>> Facilities { get; set; }

        public ReactiveProperty<bool> ShowUpdate { get; set; }

        public ReactiveProperty<string> UpdateToVersionMessage { get; set; }

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

            var result = MessageBox.Show("A restart is required to complete the update. Would you like to exit Pro now?", "Evergreen: Restart Required",
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

            view.ViewProperties["MAPURI"] = mapView.Map.URI;

            if (!(FrameworkApplication.Panes.Create(ViewPaneId, view) is WorkflowViewModel newPane)) {
                return null;
            }

            newPane.Caption = "UIC Workflow";

            return newPane;
        }

        /// <summary>
        ///     Called when the pane is initialized.
        /// </summary>
        protected override async Task InitializeAsync() {
            var uri = ((CIMGenericView)_cimView).ViewProperties["MAPURI"] as string;
            await SetMapURI(uri);

            await base.InitializeAsync();
        }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            MapSelectionChangedEvent.Unsubscribe(_subscriptionToken);

            Facilities.Dispose();
            FacilityId.Dispose();
            Model.Dispose();
            ShowUpdate.Dispose();
            UpdateToVersionMessage.Dispose();
            UpdateSelf.Dispose();

            await base.UninitializeAsync();
        }
    }
}
