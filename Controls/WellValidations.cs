using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Extensions;
using uic_addin.Services;


namespace uic_addin.Controls {
    internal class WellOperatingStatus : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            var table = LayerService.FindLayer("uicWell", MapView.Active.Map);

            if (table == null) {
                Log.Warning("Could not find well");

                throw new NullReferenceException("layer to query was not found");
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
            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(3));

            ThreadService.RunOnUiThread(() => {
                var message = $"There are {primaryKeys.Count} wells with no Operating Status record. " +
                               "They have been selected.";

                Log.Verbose("Showing notification: {message}", message);

                var selection = new Notification {
                    Message = message,
                    ImageUrl = "",
                    Title = "UIC Add-in"
                };

                FrameworkApplication.AddNotification(selection);
            });
        });
    }

    internal class Authorization : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            var wells = LayerService.FindLayer("uicWell", MapView.Active.Map);
            var filter = new QueryFilter {
                SubFields = "OBJECTID",
                WhereClause = "Authorization_FK is null"
            };

            Log.Verbose("Getting wells with no authorization record");

            var ids = new List<long>();
            using (var cursor = wells.Search(filter)) {
                while (cursor.MoveNext()) {
                    var oid = Convert.ToInt64(cursor.Current["OBJECTID"]);

                    ids.Add(oid);
                }
            }

            Log.Verbose("Found {count} wells without an Authorization", ids.Count);

            var layer = MapView.Active.Map.GetLayersAsFlattenedList()
                .FirstOrDefault(x => string.Equals(((BasicFeatureLayer)x)
                    .GetTable().GetName().SplitAndTakeLast('.'),
                    "uicWell".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            Log.Verbose("Selecting Wells");

            MapView.Active.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                { layer, ids }
            });

            Log.Verbose("Zooming to seleted");
            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(3));

            ThreadService.RunOnUiThread(() => {
                var message = $"There are {ids.Count} wells with no Authorization. " +
                               "They have been selected.";

                Log.Verbose("Showing notification: {message}", message);

                var selection = new Notification {
                    Message = message,
                    ImageUrl = "",
                    Title = "UIC Add-in"
                };

                FrameworkApplication.AddNotification(selection);
            });
        });
    }

    internal class AreaOfReview : Button {
        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            var wells = LayerService.FindLayer("uicWell", MapView.Active.Map);
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
            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(3));

            ThreadService.RunOnUiThread(() => {
                var message = $"There are {noAors.Count} wells with an AuthorizationType of IP or AP that do not " +
                               "have an Area of review polygon. They have been selected.";

                Log.Verbose("Showing notification: {message}", message);

                var selection = new Notification {
                    Message = message,
                    ImageUrl = "",
                    Title = "UIC Add-in"
                };

                FrameworkApplication.AddNotification(selection);
            });
        });
    }
}
