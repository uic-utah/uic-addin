using System;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Extensions;

namespace uic_addin.Services {
    public static class LayerService {
        public static BasicFeatureLayer FindLayer(string layerName, Map map) {
            Log.Verbose("finding feature layer {layer}", layerName);

            var layers = map.GetLayersAsFlattenedList();

            return (BasicFeatureLayer)layers.FirstOrDefault(x => string.Equals(x.Name.SplitAndTakeLast('.'),
                                                                               layerName.SplitAndTakeLast('.'),
                                                                               StringComparison
                                                                                   .InvariantCultureIgnoreCase));
        }
    }
}
