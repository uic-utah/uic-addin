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
    internal class AorAuthorization : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running area of review missing authorization validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            progressDialog.Show();

            var layerName = "UICAreaOfReview";
            var layer = LayerService.GetLayer(layerName, MapView.Active.Map);

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

            Log.Debug("Finished aor authorization Validation");
        });
    }

    internal class AorArtPen : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            /*
            The UICAreaOfReview tbl has a field NoArtPenDate.
            This is the date it was determined that there were no artificial penetrations
            If this field is empty there should be at least one UICArtPen well associated with the AOR.

            Also, remember there is a many to many relationship between UICAreaOfReview and UICArtPen
            so there isn't an AOR_FK in the UICArtPen record.
             */

            Log.Debug("Running area of review missing artificial penetration validation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            var tableName = "UICAreaOfReview";
            using (var table = LayerService.GetTableFromLayersOrTables(tableName, MapView.Active.Map)) {
                progressor.Value = 10;

                if (table == null) {
                    NotificationService.NotifyOfMissingLayer(tableName);

                    progressDialog.Hide();

                    return;
                }

                var filter = new QueryFilter {
                    SubFields = "OBJECTID",
                    WhereClause = "NoArtPenDate IS NULL"
                };

                Log.Verbose("searching for area of review records with no art pen date");

                var problems = new List<long>();
                using (var gdb = table.GetDatastore() as Geodatabase) {
                    if (gdb == null) {
                        Log.Warning("Could not get geodatabase object");

                        progressDialog.Hide();
                    }

                    Log.Verbose("Got datastore as a geodatabase");

                    Log.Verbose("Opening relationship class and selecting {table} records", tableName);
                    var dbSchema = "UDEQ.UICADMIN.";
#if DEBUG
                    dbSchema = "UIC.DBO.";
#endif
                    using (var relationshipClass = gdb.OpenDataset<RelationshipClass>($"{dbSchema}UICAreaOfReview_UICArtPen"))
                    using (var selection = table.Select(filter, SelectionType.ObjectID, SelectionOption.Normal)) {
                        progressor.Value = 40;

                        var ids = selection.GetObjectIDs().ToList();

                        if (ids.Count == 0) {
                            NotificationService.NotifyOfValidationSuccess();

                            progressDialog.Hide();

                            return;
                        }

                        Log.Verbose("Finding related records to {ids}", ids);

                        foreach (var id in ids) {
                            var rows = relationshipClass.GetRowsRelatedToOriginRows(new[] { id });
                            if (!rows.Any()) {
                                problems.Add(id);
                            } else {
                                foreach (var row in rows) {
                                    row.Dispose();
                                }
                            }
                        }

                        progressor.Value = 75;
                    }

                    if (problems.Count == 0) {
                        NotificationService.NotifyOfValidationSuccess();

                        progressDialog.Hide();

                        return;
                    }

                    var layerName = "UICAreaOfReview";
                    var layer = LayerService.GetLayer(layerName, MapView.Active.Map);

                    if (layer == null) {
                        NotificationService.NotifyOfMissingLayer(layerName);

                        progressDialog.Hide();

                        return;
                    }

                    MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                        { layer, problems }
                    });

                    progressor.Value = 100;

                    progressDialog.Hide();

                    NotificationService.NotifyOfValidationFailure(problems.Count);

                    Log.Verbose("Zooming to selected");

                    await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

                    Log.Debug("Finished aor artpen Validation");
                }
            }
        });
    }
}
