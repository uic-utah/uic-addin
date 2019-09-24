using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Extensions;
using uic_addin.Services;

namespace uic_addin.Controls {
    internal class AorAuthorization : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization missing Action Validation");

            var progressDialog = new ProgressDialog("Running Tool", "Cancel", 100, false);
            progressDialog.Show();

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                .GetTable().GetName().SplitAndTakeLast('.'),
                "UICAreaOfReview".SplitAndTakeLast('.'),
                StringComparison.InvariantCultureIgnoreCase));

            if (layer == null) {
                Log.Warning("Could not find UICAreaOfReview!");

                NotificationService.Notify("🔍 The UICAreaOfReview table could not be found. " +
                                "Please add it to your map.");

                return;
            }

            Log.Verbose("Selecting UICAreaOfReview");

            IGPResult result = null;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "Authorization_FK IS NULL");
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
                NotificationService.Notify("🚀 Every Area Of Review has an Authorization record 🚀");

                return;
            }

            NotificationService.Notify($"There are {problems} Area of Reviews with no Authorization" +
                   "They problem area of review polygons have been selected.");

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));
            progressDialog.Hide();

            Log.Debug("Finished aor authorization Validation");
        });
    }
}
