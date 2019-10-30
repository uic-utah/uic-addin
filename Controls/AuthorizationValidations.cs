using System;
using ArcGIS.Desktop.Framework.Contracts;
using uic_addin.Services;
using Serilog;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace uic_addin.Controls {
    internal class AuthorizationAction : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization missing Action Validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            const string layerName = "UICAuthorizationAction";
            var layer = LayerService.GetStandaloneTable(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            progressor.Value = 10;

            const string parentLayerName = "UICAuthorization";
            var parentLayer = LayerService.GetStandaloneTable(parentLayerName, MapView.Active.Map);

            if (parentLayer == null) {
                NotificationService.NotifyOfMissingLayer(parentLayerName);

                progressDialog.Hide();

                return;
            }

            progressor.Value = 20;

            var foreignKeys = new HashSet<string>();
            var primaryKeys = new HashSet<string>();

            using (var cursor = layer.Search(new QueryFilter {
                SubFields = "Authorization_FK"
            })) {
                while (cursor.MoveNext()){
                    var fk = Convert.ToString(cursor.Current["AUTHORIZATION_FK"]);

                    foreignKeys.Add(fk);
                }
            }

            progressor.Value = 50;

            using (var cursor = parentLayer.Search(new QueryFilter{
                SubFields = "GUID"
            })) {
                while (cursor.MoveNext()) {
                    var fk = Convert.ToString(cursor.Current["GUID"]);

                    primaryKeys.Add(fk);
                }
            }

            progressor.Value = 80;

            primaryKeys.ExceptWith(foreignKeys);

            if (primaryKeys.Count == 0) {
                NotificationService.NotifyOfValidationSuccess();

                progressDialog.Hide();

                return;
            }

            var problems = new List<long>(primaryKeys.Count);

            using (var cursor = parentLayer.Search(new QueryFilter {
                SubFields = "OBJECTID",
                WhereClause = $"GUID IN ({string.Join(",", primaryKeys.Select(x => $"'{x}'"))})"
            })) {
                while (cursor.MoveNext()) {
                    var id = cursor.Current.GetObjectID();

                    problems.Add(id);
                }
            }

            progressor.Value = 90;

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                { parentLayer, problems }
            });

            progressor.Value = 100;

            progressDialog.Hide();

            NotificationService.NotifyOfValidationFailure(problems.Count);

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            Log.Debug("Finished Authorization Validation");
        });
    }

    internal class AuthorizationMissingFacilityFk : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("running authorization missing action validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            progressDialog.Show();

            const string layerName = "UICAuthorization";
            var layer = LayerService.GetStandaloneTable(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            IGPResult result = null;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "Facility_FK IS NULL");
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

            Log.Debug("finished authorization missing facility fk validation");
        });
    }
}
