using System;
using System.Linq;
using ArcGIS.Desktop.Framework.Contracts;
using uic_addin.Services;
using Serilog;
using ArcGIS.Desktop.Mapping;
using uic_addin.Extensions;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace uic_addin.Controls {
    internal class AuthorizationAction : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization missing Action Validation");

            var progressDialog = new ProgressDialog("Finding issues...", "Cancel", 100, false);
            progressDialog.Show();

            var table = MapView.Active.Map.StandaloneTables.FirstOrDefault(x => string.Equals(
                    x.GetTable().GetName().SplitAndTakeLast('.'),
                    "UICAUTHORIZATIONACTION".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            if (table == null) {
                Log.Warning("Could not find UICAuthorizationAction!");

                NotificationService.Notify("🔍 The UICAuthorizationAction table could not be found. " +
                                "Please add it to your map.");

                return;
            }

            Log.Verbose("Selecting Authorizations");

            IGPResult result = null;
            var parameters = Geoprocessing.MakeValueArray(table, "NEW_SELECTION", "Authorization_FK IS NULL");
            var progSrc = new CancelableProgressorSource(progressDialog);

            try {
                result = await Geoprocessing.ExecuteToolAsync(
                     "management.SelectLayerByAttribute",
                     parameters,
                     null,
                     new CancelableProgressorSource(progressDialog).Progressor,
                     GPExecuteToolFlags.Default
                );
            } catch (Exception ex) {
                Log.Error(ex, "Select layer by attribute {@parameters}", parameters);

                NotificationService.Notify("The tool crashed");

                progressDialog.Hide();

                return;
            }

            var problems = Convert.ToInt32(result?.Values[1]);

            if (problems == 0) {
                NotificationService.Notify("🚀 Every Authorization has an Action record 🚀");

                return;
            }

            NotificationService.Notify($"There are {problems} Authorizations with no AuthorizationAction" +
                   "They problem authorizations have been selected.");

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));
            progressDialog.Hide();

            Log.Debug("Finished Authorization Validation");
        });
    }
}
