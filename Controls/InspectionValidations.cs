using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Services;

namespace uic_addin.Controls {
    internal class InspectionCorrection : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(() => {
            Log.Debug("running inspection validation looking for not no deficiency with a missing correction");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            var idMap = new Dictionary<string, long>();
            var primaryKeys = new HashSet<string>();
            var foreignKeys = new HashSet<string>();

            var tableName = "UICInspection";
            var relatedTableName = "UICCorrection";
            using (var table = LayerService.GetTableFromLayersOrTables(tableName, MapView.Active.Map))
            using (var relatedTable = LayerService.GetTableFromLayersOrTables(relatedTableName, MapView.Active.Map)) {
                progressor.Value = 10;

                if (table == null) {
                    NotificationService.NotifyOfMissingLayer(tableName);

                    progressDialog.Hide();

                    return;
                }

                if (relatedTable == null) {
                    NotificationService.NotifyOfMissingLayer(relatedTableName);

                    progressDialog.Hide();

                    return;
                }

                var filter = new QueryFilter {
                    SubFields = "OBJECTID,GUID",
                    WhereClause = "InspectionDeficiency!='NO'"
                };

                Log.Verbose("searching for inspections value other than no deficiency");

                using (var cursor = table.Search(filter, true)) {
                    var guidIndex = cursor.FindField("GUID");

                    while (cursor.MoveNext()) {
                        var id = cursor.Current.GetObjectID();
                        var guid = cursor.Current[guidIndex].ToString();

                        idMap[guid] = id;
                        primaryKeys.Add(guid);
                    }
                }

                progressor.Value = 60;

                Log.Verbose("built set of primary keys");

                filter = new QueryFilter {
                    SubFields = "Inspection_FK"
                };

                using (var cursor = relatedTable.Search(filter, true)) {
                    var guidIndex = cursor.FindField("Inspection_FK");

                    while (cursor.MoveNext()) {
                        var guid = cursor.Current[guidIndex].ToString();

                        foreignKeys.Add(guid);
                    }
                }

                Log.Verbose("built set of foreign keys");
                progressor.Value = 90;

                primaryKeys.ExceptWith(foreignKeys);

                Log.Information("found {count} issues", primaryKeys.Count);

                if (primaryKeys.Count == 0) {
                    NotificationService.NotifyOfValidationSuccess();

                    progressDialog.Hide();

                    return;
                }

                var problems = new List<long>(primaryKeys.Count);
                problems.AddRange(idMap.Where(x => primaryKeys.Contains(x.Key)).Select(x => x.Value));

                Log.Debug("problem records {items}", problems);

                progressor.Value = 100;

                Log.Verbose("Setting selection");

                MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                    { LayerService.GetStandaloneTable(tableName, MapView.Active.Map), problems }
                });

                progressor.Value = 100;

                progressDialog.Hide();

                NotificationService.NotifyOfValidationFailure(problems.Count);

                Log.Debug("finished inspection_correction validation");
            }
        });
    }
}
