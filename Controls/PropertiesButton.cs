using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace uic_addin.Controls {
    internal class PropertiesButton : Button {
        protected override void OnClick() => PropertySheet.ShowDialog("esri_core_optionsPropertySheet",
                                                                      "EvergreenSettings");
    }
}
