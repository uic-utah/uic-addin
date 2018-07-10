using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;
using uic_addin.Services;

namespace uic_addin.Views {
    internal class WorkflowDockPaneViewModel : DockPane {
        private const string DockPaneId = "WorkflowDockPane";
        private string _heading = "UIC Workflow Main";

        public WorkflowDockPaneViewModel() {
            FacilityId = new ReactiveProperty<string>(mode: ReactivePropertyMode.DistinctUntilChanged);
            Facilities = new ReactiveProperty<IEnumerable<string>>();
            Model = new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);

            Facilities = FacilityId.Select(async id => {
                                       if (id.Length < 4) {
                                           return Enumerable.Empty<string>();
                                       }

                                       return await QueryService.GetFacilityIdsFor(id);
                                   })
                                   .CatchIgnore()
                                   .Switch()
                                   .ToReactiveProperty();
            Model = Facilities.Select(items => {
                if (items?.Count() != 1) {
                    return null as FacilityModel;
                }

                return new FacilityModel(null);
            })
            .CatchIgnore()
            .ToReactiveProperty();
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
