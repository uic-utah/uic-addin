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
    internal class WellButton : Button {

        protected override void OnClick() => ThreadService.RunOnBackground(() => {
            var wells = LayerService.FindLayer("uicWell", MapView.Active.Map);
            var filter = new QueryFilter {
                SubFields = "OBJECTID,GUID"
            };

            var primaryKeys = new Dictionary<string, long>();

            Log.Verbose("Getting well primary keys");

            using (var cursor = wells.Search(filter)) {
                while (cursor.MoveNext()) {
                    var guid = Convert.ToString(cursor.Current["GUID"]);

                    if (string.IsNullOrEmpty(guid)) {
                        continue;
                    }

                    var id = Convert.ToInt64(cursor.Current["OBJECTID"]);

                    primaryKeys.Add(guid, id);
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

    internal class AoRButton : Button {

    }

    internal class PropertiesButton : Button {
        protected override void OnClick() => PropertySheet.ShowDialog("esri_core_optionsPropertySheet",
                                                                      "EvergreenSettings");
    }

    internal class UpdateButton : Button {
        public UpdateButton() {
            Caption = UicModule.Current.IsCurrent.Value ?
                "Up to date! 💙" :
                $"Update to {UicModule.Current.EvergreenSettings?.LatestRelease?.TagName}";
            Tooltip = UicModule.Current.EvergreenSettings?.LatestRelease?.TagName;
        }

        protected override async void OnClick() {
            if (UicModule.Current.EvergreenSettings.LatestRelease == null) {
                return;
            }

            await UicModule.Current.Evergreen.Value.Update(UicModule.Current.EvergreenSettings.LatestRelease);

            var result =
                MessageBox.Show("A restart is required to complete the update. Would you like to exit Pro now?",
                                "Evergreen: Restart Required",
                                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes) {
                await FrameworkApplication.ShutdownAsync();
            }
        }
    }
}
