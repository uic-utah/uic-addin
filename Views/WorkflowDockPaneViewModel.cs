using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Extensions;
using uic_addin.Models;
using uic_addin.Services;

namespace uic_addin.Views {
    internal class WorkflowDockPaneViewModel : DockPane {
        public const string DockPaneId = "WorkflowDockPane";
        public ICommand UseSelection { get; }
        public ICommand ZoomToFacility { get; }

        private string _heading = "UIC Workflow Main";
        private readonly SubscriptionToken _token;

        public WorkflowDockPaneViewModel() {
            FacilityId = new ReactiveProperty<string>(mode: ReactivePropertyMode.DistinctUntilChanged);
            Facilities = new ReactiveProperty<IEnumerable<string>>(mode: ReactivePropertyMode.DistinctUntilChanged);
            Model = new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);

            if (MapView.Active == null) {
                _token = MapViewInitializedEvent.Subscribe(args => SetupSubscriptions(args.MapView.Map));
                return;
            }

            SetupSubscriptions(MapView.Active.Map);

            UseSelection = new RelayCommand(async () => {
                var facilityLayer = UicModule.Layers[FacilityModel.TableName];
                var ids = await LayerService.GetSelectedIdsFor(facilityLayer);

                var selectedIds = await QueryService.GetFacilityIdsFor(ids, facilityLayer.Map);
                if (selectedIds.Count() > 1) {
                    FrameworkApplication.AddNotification(new Notification {
                        Message = $"Selected {selectedIds.Count()} facilities. Showing {selectedIds.FirstOrDefault()}"
                    });
                }

                Facilities.Value = new[] { selectedIds.FirstOrDefault() };
            }, ()=> MapView.Active.Map.SelectionCount > 0);

            ZoomToFacility = new RelayCommand(async () => {
                var facilityLayer = UicModule.Layers[FacilityModel.TableName] as BasicFeatureLayer;

                await MapView.Active.ZoomToAsync(facilityLayer, Model.Value.ObjectId, TimeSpan.FromSeconds(2));
            }, () => MapView.Active != null);
        }

        public void SetupSubscriptions(Map map) {
            if (_token != null) {
                MapViewInitializedEvent.Unsubscribe(_token);
            }

            Facilities = FacilityId.Select(async id => {
                                       if (id.Length < 4) {
                                           return Enumerable.Empty<string>();
                                       }

                                       return await QueryService.GetFacilityIdsFor(id, map);
                                   })
                                   .CatchIgnore()
                                   .Switch()
                                   .ToReactiveProperty();

            Facilities.ObserveOn(Application.Current.Dispatcher)
                      .ForEachAsync(async items => {
                          var pane = (FacilityAttributeDockpaneViewModel)FrameworkApplication
                              .DockPaneManager.Find(FacilityAttributeDockpaneViewModel.DockPaneId);

                          if (items?.Count() != 1) {
                              pane.Model.Value = new FacilityModel();

                              return;
                          }

                          var facilityLayer = UicModule.Layers[FacilityModel.TableName];
                          pane.Model.Value = Model.Value = await QueryService.GetFacilityFor(items.First());
                          await LayerService.SelectIdFromLayer(Model.Value.ObjectId, facilityLayer);

                          FacilityAttributeDockpaneViewModel.Show();
                          var wrapper = FrameworkApplication.GetPlugInWrapper("esri_editing_ShowAttributes");
                          // tool and command(Button) supports this

                          if (wrapper is ICommand command && command.CanExecute(null)) {
                              command.Execute(null);
                          }
                      }); 

            MapSelectionChangedEvent.Subscribe(async args => {
                var facilitySelection = args.Selection?.Where(x => string.Equals(x.Key.Name.SplitAndTakeLast('.'), "uicfacility",
                                                                                 StringComparison.InvariantCultureIgnoreCase));

                var facilityObjectIds = facilitySelection?.SelectMany(x => x.Value);

                if (!facilityObjectIds.Any()) {
                    return;
                }

                var facilityIds = await QueryService.GetFacilityIdsFor(facilityObjectIds, args.Map);
                if (facilityIds.Count() > 1) {
                    FrameworkApplication.AddNotification(new Notification {
                        Message = $"Selected {facilityIds.Count()} facilities. Showing {facilityIds.FirstOrDefault()}"
                    });
                }

                Facilities.Value = new[] { facilityIds.FirstOrDefault() };
            });
        }

        // Exposed Model
        public ReactiveProperty<FacilityModel> Model { get; set; }

        public ReactiveProperty<string> FacilityId { get; set; }

        public ReactiveProperty<IEnumerable<string>> Facilities { get; set; }

        public string Heading {
            get => _heading;
            set => SetProperty(ref _heading, value, () => Heading);
        }

        public static void Show() {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }
    }
}
