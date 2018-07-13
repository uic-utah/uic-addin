using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace uic_addin.Controls {
    public class FacilitySelectTool : MapTool {
        public FacilitySelectTool() {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Rectangle;
            SketchOutputMode = SketchOutputMode.Screen;
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
            FrameworkApplication.State.Activate(UicModule.FacilitySelectionState);

            return QueuedTask.Run(() => {
                MapView.Active.SelectFeatures(geometry);

                FrameworkApplication.State.Deactivate(UicModule.FacilitySelectionState);

                return true;
            });
        }
    }
}
