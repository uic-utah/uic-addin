using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;
using uic_addin.Services;

namespace uic_addin.Views {
    internal class WorkflowDockPaneViewModel : DockPane {
        private const string DockPaneId = "WorkflowDockPane";
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
                              pane.Model.Value = new FacilityModel(null);
                              return;
                          }

                          pane.Model.Value = await QueryService.GetFacilityFor(items.First());

                          FacilityAttributeDockpaneViewModel.Show();
                      });

            MapSelectionChangedEvent.Subscribe(async args => {
                var facilitySelection = args.Selection?.Where(x => string.Equals(SplitLast(x.Key.Name), "uicfacility",
                                                                                 StringComparison.InvariantCultureIgnoreCase));

                var facilityObjectIds = facilitySelection?.SelectMany(x => x.Value);

                if (!facilityObjectIds.Any()) {
                    return;
                }

                var facilityIds = await QueryService.GetFacilityIdsFor(facilityObjectIds, args.Map);
                if (facilityIds.Count() > 1) {
                    // TODO: show message
                    FrameworkApplication.AddNotification(new Notification {
                        Message = $"Selected {facilityIds.Count()} facilities. Showing {facilityIds.FirstOrDefault()}"
                    });
                }

                Facilities.Value = new[] { facilityIds.FirstOrDefault() };
            });
        }

        private static string SplitLast(string x) {
            if (!x.Contains('.')) {
                return x;
            }

            return x.Split('.').Last();
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
