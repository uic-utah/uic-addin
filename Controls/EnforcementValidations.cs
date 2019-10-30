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
    internal class EnforcementDate : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(() => {
            Log.Debug("Running enforcement with older date than violation");

            var progressDialog = new ProgressDialog("🔍 Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            var tableName = "UICEnforcement";
            using (var table = LayerService.GetTableFromLayersOrTables(tableName, MapView.Active.Map)) {
                progressor.Value = 10;

                if (table == null) {
                    NotificationService.NotifyOfMissingLayer(tableName);

                    progressDialog.Hide();

                    return;
                }

                var filter = new QueryFilter {
                    SubFields = "OBJECTID,EnforcementDate",
                    WhereClause = "EnforcementDate IS NOT NULL"
                };

                Log.Verbose("searching for enforcements with an enforcement date");

                var problems = new List<long>();
                using (var gdb = table.GetDatastore() as Geodatabase) {
                    if (gdb == null) {
                        Log.Warning("Could not get geodatabase object");

                        progressDialog.Hide();
                    }

                    Log.Verbose("Got datastore as a geodatabase");

                    Log.Verbose("Opening relationship class and selecting {table} records", tableName);

                    var dbSchema = LayerService.GetDbSchema(MapView.Active.Map);

                    Log.Verbose("Using db and schema {schema}", dbSchema);

                    using (var relationshipClass = gdb.OpenDataset<RelationshipClass>($"{dbSchema}UICViolationToEnforcement"))
                    using (var cursor = table.Search(filter, true)) {
                        progressor.Value = 40;

                        var dateMap = new Dictionary<long, DateTime>();
                        while (cursor.MoveNext()) {
                            var record = cursor.Current;
                            var fieldIndex = record.FindField("EnforcementDate");

                            dateMap[record.GetObjectID()] = DateTime.Parse(record[fieldIndex].ToString());
                        }

                        progressor.Value = 60;

                        if (dateMap.Keys.Count == 0) {
                            NotificationService.NotifyOfValidationSuccess();

                            progressDialog.Hide();

                            return;
                        }

                        Log.Verbose("Finding related records to {ids} records", dateMap.Keys.Count);

                        foreach (var id in dateMap.Keys) {
                            var rows = relationshipClass.GetRowsRelatedToDestinationRows(new[] { id });
                            var enforcementDate = dateMap[id];

                            foreach (var row in rows) {
                                var violationIndex = row.FindField("ViolationDate");
                                if (violationIndex == -1) {
                                    Log.Verbose("Field not found {field}", "ViolationDate");
                                    continue;
                                }

                                var violationString = Convert.ToString(row[violationIndex]);

                                if(violationString is null) {
                                    row.Dispose();

                                    continue;
                                }

                                var violationDate = DateTime.Parse(violationString);

                                if (enforcementDate < violationDate) {
                                    problems.Add(id);
                                }

                                row.Dispose();
                            }
                        }

                        progressor.Value = 75;
                    }

                    if (problems.Count == 0) {
                        NotificationService.NotifyOfValidationSuccess();

                        progressDialog.Hide();

                        return;
                    }

                    var layerName = "UICEnforcement";
                    var mapMember = LayerService.GetStandaloneTable(layerName, MapView.Active.Map);

                    if (mapMember == null) {
                        NotificationService.NotifyOfMissingLayer(layerName);

                        progressDialog.Hide();

                        return;
                    }

                    MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                        { mapMember, problems }
                    });

                    progressor.Value = 100;

                    progressDialog.Hide();

                    NotificationService.NotifyOfValidationFailure(problems.Count);

                    Log.Debug("Finished enforcement date < violation date validation");
                }
            }
        });
    }
}
