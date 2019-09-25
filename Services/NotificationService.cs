using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using Serilog;

namespace uic_addin.Services {
    public static class NotificationService {
        public static int id = 0;
        public static string[] singular = new[] { "is", "record", "has" };
        public static string[] plural = new[] { "are", "records", "have" };

        public static void Notify(string message, string title = "UIC Add-in") => ThreadService.RunOnUiThread(() => {
            Log.Verbose("Showing notification: {id} {message}", id, message);

            var notification = new NotificationItem($"{id++}", false, $"{title} {message}", NotificationType.Information);

            NotificationManager.AddNotification(notification);
            FrameworkApplication.AddNotification(new Notification {
                Message = message,
                ImageUrl = "",
                Title = title
            });
        });

        public static void NotifyOfMissingLayer(string layer, string title = "UIC Add-in") => ThreadService.RunOnUiThread(() => {
            Log.Warning("Missing {layer} in map pane", layer);

            Notify($"🔍 The table, {layer}, could not be found in your current map pane. "
            + "Please add it to your map to use this tool.", title);
        });

        public static void NotifyOfGpFailure(IGPResult result, IReadOnlyList<string> parameters) {
            Log.Error("GP Tool failure: {@parameters} {messages}", parameters, result.ErrorMessages);

            Notify($"💩 The tool failed to execute. {string.Join(". ", result.ErrorMessages.Select(x => x.Text))}");
        }

        public static void NotifyOfGpCrash(Exception error, IReadOnlyList<string> parameters) {
            Log.Error(error, "Select layer by attribute {@parameters}", parameters);

            Notify($"💩 The tool crashed. {error.Message}");
        }

        public static void NotifyOfValidationFailure(int occurrences) {
            var words = occurrences == 1 ? singular : plural;

            Notify($"🔥 There {words[0]} {occurrences} {words[1]} failing this validation. " +
                   $"The problem {words[1]} {words[2]} been selected.");
        }

        public static void NotifyOfValidationSuccess() {
            Notify($"✔️ Every record passed validation! 🚀");
        }
    }
}
