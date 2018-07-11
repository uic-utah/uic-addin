using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;


namespace uic_addin.Views {
    internal class FacilityAttributeDockpaneViewModel : DockPane {
        public const string DockPaneId = "FacilityAttributeDockpane";
        public ReactiveProperty<FacilityModel> Model { get; set; }

        public FacilityAttributeDockpaneViewModel() {
            Model = new ReactiveProperty<FacilityModel>(mode: ReactivePropertyMode.DistinctUntilChanged);
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show() {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading {
            get => _heading;
            set => SetProperty(ref _heading, value, () => Heading);
        }
    }
}
