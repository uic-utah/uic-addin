using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Services;


namespace uic_addin.Controls {
    internal class WellOperatingStatus : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(async () => {
            Log.Debug("running well operating status validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            const string layerName = "UICWell";
            var layer = LayerService.GetLayer(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            IGPResult result;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "not exists (select 1 from UICWellOperatingStatus b where b.WELL_FK = UICWell.GUID)");
            var progSrc = new CancelableProgressorSource(progressDialog);

            Log.Verbose("management.SelectLayerByAttribute on {layer} with {@params}", layerName, parameters);

            try {
                result = await Geoprocessing.ExecuteToolAsync(
                    "management.SelectLayerByAttribute",
                    parameters,
                    null,
                    new CancelableProgressorSource(progressDialog).Progressor,
                    GPExecuteToolFlags.Default);
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

            Log.Debug("Finished Well Operating Status Validation");
        });
    }

    internal class Authorization : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization Validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            progressDialog.Show();

            const string layerName = "UICWell";
            var layer = LayerService.GetLayer(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            IGPResult result;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "Authorization_FK IS NULL");
            var progSrc = new CancelableProgressorSource(progressDialog);

            Log.Verbose("management.SelectLayerByAttribute on {layer} with {@params}", layerName, parameters);

            try {
                result = await Geoprocessing.ExecuteToolAsync(
                    "management.SelectLayerByAttribute",
                    parameters,
                    null,
                    new CancelableProgressorSource(progressDialog).Progressor,
                    GPExecuteToolFlags.Default);
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

    internal class AreaOfReview : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            Log.Debug("Running Area of Review Validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            var authorizations = new Dictionary<string, List<long>>();
            var noAreaOfReview = new HashSet<long>();

            var tableName = "UICWell";
            using (var table = LayerService.GetTableFromLayersOrTables("UICWell", MapView.Active.Map)) {
                progressor.Value = 10;

                if (table == null) {
                    NotificationService.NotifyOfMissingLayer(tableName);

                    progressDialog.Hide();

                    return;
                }

                var filter = new QueryFilter {
                    SubFields = "OBJECTID,AUTHORIZATION_FK",
                    WhereClause = "Authorization_FK is not null AND AOR_FK is null"
                };

                Log.Verbose("Getting wells with an authorization but no area of review");

                using (var cursor = table.Search(filter)) {
                    while (cursor.MoveNext()) {
                        var oid = Convert.ToInt64(cursor.Current["OBJECTID"]);
                        var guid = Convert.ToString(cursor.Current["AUTHORIZATION_FK"]);

                        if (authorizations.ContainsKey(guid)) {
                            authorizations[guid].Add(oid);

                            continue;
                        }

                        authorizations.Add(guid, new List<long> { oid });
                    }
                }
            }

            Log.Verbose("Got authorizations {dict}", authorizations);

            progressor.Value = 40;

            tableName = "UICAuthorization";
            var table2 = LayerService.GetStandaloneTable(tableName, MapView.Active.Map);
            progressor.Value = 50;

            if (table2 == null) {
                NotificationService.NotifyOfMissingLayer(tableName);

                progressDialog.Hide();

                return;
            }

            var filter2 = new QueryFilter {
                SubFields = "GUID",
                WhereClause = $"AuthorizationType IN ('IP', 'AP') AND GUID IN ({string.Join(",", authorizations.Keys.Select(x => $"'{x}'"))})"
            };

            Log.Verbose("searching for well authorizations with type IP or AP");

            using (var cursor = table2.Search(filter2)) {
                while (cursor.MoveNext()) {
                    var guid = Convert.ToString(cursor.Current["GUID"]);

                    authorizations[guid].ForEach(x => noAreaOfReview.Add(x));
                }
            }

            Log.Verbose("got the guids {dict}", authorizations);

            progressor.Value = 90;

            if (noAreaOfReview.Count == 0) {
                NotificationService.NotifyOfValidationSuccess();

                progressDialog.Hide();

                return;
            }

            Log.Verbose("Found {count} wells with no AOR with an authorization of IP or AP", noAreaOfReview.Count);

            var layerName = "UICWell";
            var layer = LayerService.GetLayer(layerName, MapView.Active.Map);

            if (layer == null) {
                NotificationService.NotifyOfMissingLayer(layerName);

                progressDialog.Hide();

                return;
            }

            Log.Verbose("Selecting Wells");

            progressor.Value = 95;

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                { layer, noAreaOfReview.ToList() }
            });

            progressor.Value = 100;

            progressDialog.Hide();

            NotificationService.NotifyOfValidationFailure(noAreaOfReview.Count);

            Log.Verbose("Zooming to selected");

            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            Log.Debug("Finished Authorization Validation");
        });
    }
}
