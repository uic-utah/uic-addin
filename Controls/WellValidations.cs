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

            var progressDialog = new ProgressDialog("Running Tool", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            if (layer == null) {
                Log.Warning("Could not find well");

                NotificationService.Notify("The uicWell table could not be found. " +
                            "Please add it to your map.");

                progressDialog.Hide();

                return;
            }

            Log.Verbose("Selecting wells");

            IGPResult results;
            var parameters = Geoprocessing.MakeValueArray(layer, "NEW_SELECTION", "not exists (select 1 from UIC.DBO.UICWellOperatingStatus b where b.WELL_FK = UIC.DBO.UICWell.GUID)");
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
                NotificationService.Notify("🚀 Every Well has an Operating Status record 🚀");
            }

            NotificationService.Notify($"There are {problems} wells with no Operating Status record. " +
                                       "They problem wells have been selected.");

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));
            progressDialog.Hide();

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

            Log.Verbose("Zooming to selected");

            await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));
            progressDialog.Hide();

            Log.Debug("Finished Authorization Validation");
        });
    }

    internal class AreaOfReview : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            var progressDialog = new ProgressDialog("Running Tool", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            Log.Debug("Running Area of Review Validation");

            var wells = LayerService.FindLayer("uicWell", MapView.Active.Map);

            progressor.Value = 10;

            if (wells == null) {
                Log.Warning("Could not find well");

                NotificationService.Notify("The uicWell table could not be found. " +
                            "Please add it to your map.");

                progressDialog.Hide();

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

            progressor.Value = 40;

            var authorization = LayerService.FindLayer("uicAuthorization", MapView.Active.Map);

            progressor.Value = 50;

            if (authorization == null) {
                Log.Warning("Could not find uicAuthorization!");

                NotificationService.Notify("The uicAuthorization table could not be found. " +
                            "Please add it to your map.");

                progressDialog.Hide();

                return;
            }

            filter = new QueryFilter {
                SubFields = "GUID",
                WhereClause = $"AuthorizationType IN ('IP', 'AP') AND GUID IN ({string.Join(",", authorizations.Keys.Select(x => $"'{x}'"))})"
            };

            Log.Verbose("Getting well authorizations with type IP or AP");

            var noAreaOfReview = new HashSet<long>();

            using (var cursor = authorization.Search(filter)) {
                while (cursor.MoveNext()) {
                    var guid = Convert.ToString(cursor.Current["GUID"]);

                    authorizations[guid].ForEach(x => noAreaOfReview.Add(x));
                }
            }

            progressor.Value = 90;

            Log.Verbose("Found {count} wells with no AOR with an authorization of IP or AP", noAreaOfReview.Count);

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            Log.Verbose("Selecting Wells");

            progressor.Value = 95;

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                { layer, noAreaOfReview.ToList() }
            });

            progressor.Value = 100;

            progressDialog.Hide();

            Log.Verbose("Zooming to selected");
            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

            NotificationService.Notify($"There are {noAreaOfReview.Count} wells with an AuthorizationType of IP or AP that do not " +
                            "have an Area of review polygon. They have been selected.");

            Log.Debug("Finished Authorization Validation");
        });
    }
}
