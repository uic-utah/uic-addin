using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using uic_addin.Views;

namespace uic_addin.Controls {
    internal class ShowWorkflowPane : Button {
        protected override void OnClick() => WorkflowViewModel.Create(MapView.Active);
    }
}
