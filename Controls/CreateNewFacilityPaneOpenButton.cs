using ArcGIS.Desktop.Framework.Contracts;

namespace uic_addin.Controls
{
    /// <summary>
    ///     Button implementation to create a new instance of the pane and activate it.
    /// </summary>
    internal class CreateNewFacilityPaneOpenButton : Button {
        protected override void OnClick() {
            CreateNewFacilityPaneViewModel.Create();
        }
    }
}