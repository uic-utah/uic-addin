using System;
using System.Collections.Generic;
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
        private readonly SubscriptionToken _token;


        private string _heading = "UIC Workflow Main";

        public WorkflowDockPaneViewModel() {
            FacilityId = new ReactiveProperty<string>(mode: ReactivePropertyMode.DistinctUntilChanged);
            Facilities = new ReactiveProperty<IEnumerable<string>>(mode: ReactivePropertyMode.DistinctUntilChanged);
            Model = new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);

            UseSelection = new RelayCommand(async () => {
                var facilityLayer = UicModule.Layers[FacilityModel.TableName];
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
                var facilityLayer = UicModule.Layers[FacilityModel.TableName] as BasicFeatureLayer;

                await MapView.Active.ZoomToAsync(facilityLayer, Model.Value.ObjectId, TimeSpan.FromMilliseconds(250));
            }, () => {
                if (MapView.Active == null) {
                    return false;
                }

                return Model?.Value?.ObjectId > 0;
            });

            ShowAttributeEditorForSelectedRecord = new RelayCommand(() => {
                var wrapper = FrameworkApplication.GetPlugInWrapper("esri_editing_ShowAttributes");

                if (wrapper is ICommand command && command.CanExecute(null)) {
                    command.Execute(null);
                }
            }, () => MapView.Active?.Map.SelectionCount > 0);

            if (MapView.Active == null) {
                _token = MapViewInitializedEvent.Subscribe(args => SetupSubscriptions(args.MapView.Map));
                return;
            }

            SetupSubscriptions(MapView.Active.Map);
        }

        public ICommand UseSelection { get; }
        public ICommand ZoomToFacility { get; }
        public ICommand ShowAttributeEditorForSelectedRecord { get; }

        // Exposed Model
        public ReactiveProperty<FacilityModel> Model { get; set; }

        public ReactiveProperty<string> FacilityId { get; set; }

        public ReactiveProperty<IEnumerable<string>> Facilities { get; set; }

        public string Heading {
            get => _heading;
            set => SetProperty(ref _heading, value, () => Heading);
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
                          if (items?.Count() != 1) {
                              Model.Value = new FacilityModel();
                              
                              return;
                          }

                          Model.Value = await QueryService.GetFacilityFor(items.First());

                          var selection = await ThreadService.RunOnBackground(MapView.Active.Map.GetSelection);

                          if (selection.Count(x => x.Key.Name == FacilityModel.TableName) != 0) {
                              return;
                          }

                          var facilityLayer = UicModule.Layers[FacilityModel.TableName];
                          await LayerService.SetSelectionFromId(Model.Value.ObjectId, facilityLayer);
                      });

            MapSelectionChangedEvent.Subscribe(async args => {
                if (!FrameworkApplication.State.Contains(UicModule.FacilitySelectionState)) {
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

        public static void Show() {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }
    }
}
