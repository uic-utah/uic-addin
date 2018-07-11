using System;
using System.Windows;
using ArcGIS.Desktop.Framework;

namespace uic_addin.Services {
    public class ThreadService {
        public static bool IsOnUiThread => FrameworkApplication.TestMode ||
                                           Application.Current.Dispatcher.CheckAccess();

        internal static void RunOnUiThread(Action action) {
            try {
                if (IsOnUiThread) {
                    action();
                } else {
                    Application.Current.Dispatcher.Invoke(action);
                }
            } catch (Exception) {
            }
        }
    }
}
