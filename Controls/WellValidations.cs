using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Extensions;
using uic_addin.Services;


namespace uic_addin.Controls {
    internal class WellOperatingStatus : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Well Operating Status Validation");

            var table = LayerService.FindLayer("uicWell", MapView.Active.Map);

            if (table == null) {
                Log.Warning("Could not find well");

                NotificationService.Notify("The uicWell table could not be found. " +
                            "Please add it to your map.");

                return;
            }

            var filter = new QueryFilter {
                SubFields = "OBJECTID,GUID"
            };

            var primaryKeys = new Dictionary<string, long>();

            Log.Verbose("Getting well primary keys");

            using (var cursor = table.Search(filter)) {
                while (cursor.MoveNext()) {
                    var guid = Convert.ToString(cursor.Current["GUID"]);

                    if (string.IsNullOrEmpty(guid)) {
                        continue;
                    }

                    var oid = Convert.ToInt64(cursor.Current["OBJECTID"]);

                    primaryKeys.Add(guid, oid);
                }
            }

            Log.Verbose("Found {count} wells", primaryKeys.Count);

            var operatingStatus = LayerService.FindLayer("uicWellOperatingStatus", MapView.Active.Map);

            if (operatingStatus == null) {
                Log.Warning("Could not find uicWellOperatingStatus!");

                NotificationService.Notify("The uicWellOperatingStatus table could not be found. " +
                            "Please add it to your map.");

                return;
            }

            var filter2 = new QueryFilter {
                SubFields = "WELL_FK"
            };

            Log.Verbose("Getting well operating status fk's");

            using (var cursor = operatingStatus.Search(filter2)) {
                while (cursor.MoveNext()) {
                    var parent = cursor.Current["WELL_FK"].ToString();

                    if (primaryKeys.ContainsKey(parent)) {
                        primaryKeys.Remove(parent);
                    }
                }
            }

            Log.Verbose("Found {count} wells without operating status", primaryKeys.Count);

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            Log.Verbose("Selecting Wells");

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                { layer, primaryKeys.Select(x => x.Value).ToList() }
            });

            Log.Verbose("Zooming to seleted");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            NotificationService.Notify($"There are {primaryKeys.Count} wells with no Operating Status record. " +
                            "They have been selected.");

            Log.Debug("Finished Well Operating Status Validation");
        });
    }

    internal class Authorization : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization Validation");

            var progressDialog = new ProgressDialog("Running Tool", "Cancel", 100, false);
            progressDialog.Show();

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            if (layer == null) {
                Log.Warning("Could not find well");

                NotificationService.Notify("🔍 The uicWell table could not be found. " +
                                           "Please add it to your map.");

                return;
            }

            Log.Verbose("Selecting Wells");

            IGPResult results;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "Authorization_FK IS NULL");
            var progSrc = new CancelableProgressorSource(progressDialog);

            try {
                results = await Geoprocessing.ExecuteToolAsync(
                    "management.SelectLayerByAttribute",
                    parameters,
                    null,
                    new CancelableProgressorSource(progressDialog).Progressor,
                    GPExecuteToolFlags.Default);
            } catch (Exception ex) {
                Log.Error(ex, "Select layer by attribute {@parameters}", parameters);

                NotificationService.Notify("The tool crashed");

                progressDialog.Hide();

                return;
            }

            var problems = Convert.ToInt32(results.Values[1]);

            if (problems == 0) {
                NotificationService.Notify("🚀 Every Well has an Authorization record 🚀");
            }

            NotificationService.Notify($"There are {problems} wells with no Authorization. " +
                                       "They problem wells have been selected.");

            Log.Verbose("Zooming to seleted");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));
            progressDialog.Hide();

            Log.Debug("Finished Authorization Validation");
        });
    }

    internal class AreaOfReview : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            Log.Debug("Running Area of Review Validation");

            var wells = LayerService.FindLayer("uicWell", MapView.Active.Map);

            if (wells == null) {
                Log.Warning("Could not find well");

                NotificationService.Notify("The uicWell table could not be found. " +
                            "Please add it to your map.");

                return;
            }

            var filter = new QueryFilter {
                SubFields = "OBJECTID,AUTHORIZATION_FK",
                WhereClause = "Authorization_FK is not null AND AOR_FK is null"
            };

            Log.Verbose("Getting wells with an authorization but no area of review");

            var authorizations = new Dictionary<string, List<long>>();
            using (var cursor = wells.Search(filter)) {
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

            var authorization = LayerService.FindLayer("uicAuthorization", MapView.Active.Map);

            if (authorization == null) {
                Log.Warning("Could not find uicAuthorization!");

                NotificationService.Notify("The uicAuthorization table could not be found. " +
                            "Please add it to your map.");

                return;
            }

            filter = new QueryFilter {
                SubFields = "GUID",
                WhereClause = $"AuthorizationType IN ('IP', 'AP') AND GUID IN ({string.Join(",", authorizations.Keys.Select(x => $"'{x}'"))})"
            };

            Log.Verbose("Getting well authorizations with type IP or AP");

            var noAors = new HashSet<long>();

            using (var cursor = authorization.Search(filter)) {
                while (cursor.MoveNext()) {
                    var guid = Convert.ToString(cursor.Current["GUID"]);

                    authorizations[guid].ForEach(x => noAors.Add(x));
                }
            }

            Log.Verbose("Found {count} wells with no AOR with an authorization of IP or AP", noAors.Count);

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            Log.Verbose("Selecting Wells");

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                    { layer, noAors.ToList() }
                });

            Log.Verbose("Zooming to seleted");
            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            NotificationService.Notify($"There are {noAors.Count} wells with an AuthorizationType of IP or AP that do not " +
                            "have an Area of review polygon. They have been selected.");

            Log.Debug("Finished Authorization Validation");
        });
    }
}
