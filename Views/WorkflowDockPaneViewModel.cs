using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace uic_addin.Views {
    internal class WorkflowDockPaneViewModel : DockPane {
        private const string DockPaneId = "WorkflowDockPane";

        /// <summary>
        ///     Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "UIC Workflow Main";

        public WorkflowDockPaneViewModel() {
        }

        public string Heading {
            get => _heading;
            set { SetProperty(ref _heading, value, () => Heading); }
        }

        /// <summary>
        ///     Show the DockPane.
        /// </summary>
        internal static void Show() {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }
    }
}
