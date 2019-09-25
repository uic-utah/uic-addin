using System;
using ArcGIS.Desktop.Framework.Contracts;
using uic_addin.Services;
using Serilog;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Linq;

namespace uic_addin.Controls {
    internal class AuthorizationAction : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization missing Action Validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            progressDialog.Show();

            const string layerName = "UICAuthorizationAction";
            var layer = LayerService.GetStandaloneTable(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            IGPResult result = null;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "Authorization_FK IS NULL");
            var progSrc = new CancelableProgressorSource(progressDialog);

            Log.Verbose("management.SelectLayerByAttribute on {layer} with {@params}", layerName, parameters);

            try {
                result = await Geoprocessing.ExecuteToolAsync(
                     "management.SelectLayerByAttribute",
                     parameters,
                     null,
                     new CancelableProgressorSource(progressDialog).Progressor,
                     GPExecuteToolFlags.Default
                );
            } catch (Exception ex) {
                NotificationService.NotifyOfGpCrash(ex, parameters);

                progressDialog.Hide();

                return;
            }

            if (result.IsFailed || string.IsNullOrEmpty(result?.ReturnValue)) {
                NotificationService.NotifyOfGpFailure(result, parameters);

                progressDialog.Hide();

                return;
            }

            var problems = Convert.ToInt32(result?.Values[1]);

            if (problems == 0) {
                NotificationService.NotifyOfValidationSuccess();

                progressDialog.Hide();

                return;
            }

            progressDialog.Hide();

            NotificationService.NotifyOfValidationFailure(problems);

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            Log.Debug("Finished Authorization Validation");
        });
    }
}
