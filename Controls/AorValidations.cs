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
    internal class AorAuthorization : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running area of review missing authorization validation");

            var progressDialog = new ProgressDialog("Finding issues...", "Cancel", 100, false);
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

                progressDialog.Hide();

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

            var progressDialog = new ProgressDialog("Finding issues...", "Cancel", 100, false);
            var progressor = new CancelableProgressorSource(progressDialog).Progressor;
            progressDialog.Show();

            using (var table = LayerService.FindLayer("UICAreaOfReview", MapView.Active.Map)) {
                progressor.Value = 10;

                if (table == null) { // || artPenLayer == null) {
                    Log.Warning("Could not find layer!");

                    NotificationService.Notify("🔍 The UICAreaOfReview or UICArtPen layers could not be found. " +
                                    "Please add them to your map.");

                    progressDialog.Hide();

                    return;
                }

                var filter = new QueryFilter {
                    SubFields = "OBJECTID",
                    WhereClause = "NoArtPenDate IS NULL"
                };

                Log.Verbose("Finding area of review records with no art pen date");

                var problems = new List<long>();
                using (var gdb = table.GetDatastore() as Geodatabase) {
                    if (gdb == null) {
                        Log.Warning("Could not get geodatabase object!");

                        progressDialog.Hide();
                    }

                    using (var relationshipClass = gdb.OpenDataset<RelationshipClass>("UICAreaOfReview_UICArtPen"))
                    using (var selection = table.Select(filter, SelectionType.ObjectID, SelectionOption.Normal)) {
                        progressor.Value = 40;

                        var ids = selection.GetObjectIDs().ToList();

                        if (ids.Count == 0) {
                            NotificationService.Notify("There are no area of review polygons with an empty `NoArtPenDate`");

                            progressDialog.Hide();

                            return;
                        }

                        foreach (var id in ids) {
                            if (!relationshipClass.GetRowsRelatedToOriginRows(new[] { id }).Any()) {
                                problems.Add(id);
                            }
                        }

                        progressor.Value = 75;
                    }

                    if (problems.Count == 0) {
                        NotificationService.Notify("🚀 Every Area Of Review with a NoArtPenDate has an ArtPen record 🚀");

                        progressDialog.Hide();

                        Log.Debug("Finished aor artpen Validation");

                        return;
                    }

                    var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                            .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                            .GetTable().GetName().SplitAndTakeLast('.'),
                            "UICAreaOfReview".SplitAndTakeLast('.'),
                            StringComparison.InvariantCultureIgnoreCase));

                    MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                        { layer, problems }
                    });

                    progressor.Value = 100;

                    progressDialog.Hide();

                    Log.Verbose("Zooming to selected");
                    await MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(1.5));

                    NotificationService.Notify($"There are {problems.Count} area of reviews that should have artificial penetration records");

                    Log.Debug("Finished aor artpen Validation");
                }
            }
        });
    }
}
