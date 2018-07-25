using ArcGIS.Desktop.Framework.Contracts;
using uic_addin.Views;

namespace uic_addin.Controls
{
    /// <summary>
    ///     Button implementation to create a new instance of the pane and activate it.
    /// </summary>
    internal class NaicsFinderPaneOpenButton : Button {
        protected override void OnClick() => NaicsFinderPaneViewModel.Create();
    }
}
