using ArcGIS.Desktop.Framework;
using Serilog;

namespace uic_addin.Services {
    public static class NotificationService {
        public static void Notify(string message, string title="UIC Add-in") => ThreadService.RunOnUiThread(() => {
             Log.Verbose("Showing notification: {message}", message);

            var selection = new Notification {
                Message = message,
                ImageUrl = "",
                Title = title
            };

            NotificationManager.AddNotification(new NotificationItem(title, false, message, NotificationType.Information));
            FrameworkApplication.AddNotification(selection);
        });
    }
}
