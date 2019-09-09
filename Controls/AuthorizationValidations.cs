using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Core.Data;
using uic_addin.Services;
using Serilog;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using uic_addin.Extensions;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace uic_addin.Controls {
    internal class AuthorizationAction : Button {
        protected override async void OnClick() => await ThreadService.RunOnBackground(async () => {
            Log.Debug("Running Authorization missing Action Validation");

            var table = MapView.Active.Map.StandaloneTables.FirstOrDefault(x => string.Equals(
                    x.GetTable().GetName().SplitAndTakeLast('.'),
                    "UICAUTHORIZATIONACTION".SplitAndTakeLast('.'),
                    StringComparison.InvariantCultureIgnoreCase));

            if (table == null) {
                Log.Warning("Could not find UICAuthorizationAction!");

                ThreadService.RunOnUiThread(() => {
                    var message = "🔍 The UICAuthorizationAction table could not be found. " +
                                "Please add it to your map.";

                    Log.Verbose("Showing notification: {message}", message);

                    var selection = new Notification {
                        Message = message,
                        ImageUrl = "",
                        Title = "UIC Add-in"
                    };

                    FrameworkApplication.AddNotification(selection);
                });

                return;
            }

            Log.Verbose("Selecting Authorizations");

            var parameters = Geoprocessing.MakeValueArray(table, "NEW_SELECTION", "Authorization_FK is null");
            IGPResult result = null;
            try {
               result = await Geoprocessing.ExecuteToolAsync(
                    "management.SelectLayerByAttribute",
                    parameters
               );
            } catch(Exception e) {
                return;
            }

            var problems = Convert.ToInt32(result?.Values[1]);

            if (problems == 0) {
                ThreadService.RunOnUiThread(() => {
                    var message = "🚀 Every Authorization has an Action record 🚀";

                    Log.Verbose("Showing notification: {message}", message);

                    var selection = new Notification {
                        Message = message,
                        ImageUrl = "",
                        Title = "UIC Add-in"
                    };

                    FrameworkApplication.AddNotification(selection);
                });

                return;
            }

            Log.Verbose("Zooming to seleted");

            MapView.Active.ZoomToSelectedAsync(TimeSpan.FromSeconds(3));

            ThreadService.RunOnUiThread(() => {
                var message = $"There are {problems} Authorizations with no AuthorizationAction" +
                               "They problem authorizations have been selected.";

                Log.Verbose("Showing notification: {message}", message);

                var selection = new Notification {
                    Message = message,
                    ImageUrl = "",
                    Title = "UIC Add-in"
                };

                FrameworkApplication.AddNotification(selection);
            });

            Log.Debug("Finished Authorization Validation");
        });
    }
}
