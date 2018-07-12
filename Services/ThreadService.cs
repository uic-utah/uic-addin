using System;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace uic_addin.Services {
    public class ThreadService {
        public static bool IsOnUiThread => FrameworkApplication.TestMode ||
                                           Application.Current.Dispatcher.CheckAccess();

        public static void RunOnUiThread(Action action) {
            try {
                if (IsOnUiThread) {
                    action();
                } else {
                    Application.Current.Dispatcher.Invoke(action);
                }
            } catch (Exception) {
            }
        }

        public static Task RunOnBackground(Action action) => QueuedTask.Run(action);

        public static Task<T> RunOnBackground<T>(Func<T> func) => QueuedTask.Run(func);
    }
}
