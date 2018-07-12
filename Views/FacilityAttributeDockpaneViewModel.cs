using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Reactive.Bindings;
using uic_addin.Models;


namespace uic_addin.Views {
    internal class FacilityAttributeDockpaneViewModel : DockPane {
        public const string DockPaneId = "FacilityAttributeDockpane";
        public ReactiveProperty<FacilityModel> Model { get; set; }

        public FacilityAttributeDockpaneViewModel() {
            Model = new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);
        }

        internal static void Show() {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        private string _heading = "My DockPane";
        public string Heading {
            get => _heading;
            set => SetProperty(ref _heading, value, () => Heading);
        }
    }
}
