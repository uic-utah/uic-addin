using System.Windows;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace uic_addin.Controls {
    internal class UpdateButton : Button {
        public UpdateButton() {
            Caption = UicModule.Current.IsCurrent.Value ?
                "Up to date! 💙" :
                $"Update to {UicModule.Current.EvergreenSettings?.LatestRelease?.TagName}";
            Tooltip = UicModule.Current.EvergreenSettings?.LatestRelease?.TagName;
        }

        protected override async void OnClick() {
            if (UicModule.Current.EvergreenSettings.LatestRelease == null) {
                return;
            }

            await UicModule.Current.Evergreen.Value.Update(UicModule.Current.EvergreenSettings.LatestRelease);

            var result =
                MessageBox.Show("A restart is required to complete the update. Would you like to exit Pro now?",
                                "Evergreen: Restart Required",
                                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes) {
                await FrameworkApplication.ShutdownAsync();
            }
        }
    }
}
