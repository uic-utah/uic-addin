using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace uic_addin {
    internal class UicModule : Module {
        private static UicModule _this;

        /// <summary>
        ///     Retrieve the singleton instance to this module here
        /// </summary>
        public static UicModule Current => _this ?? (_this =
                                               (UicModule)FrameworkApplication.FindModule("UICModule"));

        protected override bool CanUnload() {
            //return false to ~cancel~ Application close
            return true;
        }
    }
}
