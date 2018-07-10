using ArcGIS.Desktop.Framework.Contracts;
using uic_addin.Views;

namespace uic_addin.Controls
{
    internal class WorkflowDockPaneShowButton : Button {
        protected override void OnClick() {
            WorkflowDockPaneViewModel.Show();
        }
    }
}
