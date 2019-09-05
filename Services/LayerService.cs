using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using Serilog;
using uic_addin.Extensions;

namespace uic_addin.Services {
    public static class LayerService {
        public static Table FindLayer(string layerName, Map map) {
            Log.Verbose("finding feature layer {layer}", layerName);

            Table filter(string name, IEnumerable<Table> l) {
                return l.FirstOrDefault(x => string.Equals(x.GetName()
                                                            .SplitAndTakeLast('.'), name.SplitAndTakeLast('.'),
                                                            StringComparison.InvariantCultureIgnoreCase));
            };

            var layers = map.GetLayersAsFlattenedList().Where(x => x.MapLayerType == ArcGIS.Core.CIM.MapLayerType.Operational)
                                                       .Select(x => ((BasicFeatureLayer)x).GetTable());
            var layer = filter(layerName, layers);

            if (layer != null) {
                Log.Verbose("found {layer}", layer.GetName());

                return layer;
            }

            Log.Verbose("missed feature classes checking tables");

            var tables = map.StandaloneTables.Select(x => x.GetTable());
            var table = filter(layerName, tables);

            if (table == null) {
                Log.Verbose("missed table also");

                return null;
            }

            Log.Verbose("found {layer}", table.GetName());

            return table;
        }

        public static async Task<IEnumerable<long>> GetSelectedIdsFor(MapMember layer) {
            if (layer == null) {
                return Enumerable.Empty<long>();
            }

            var selection = await GetSelection(layer.Map);

            return selection
                .SelectMany(pair => {
                    if (pair.Key is BasicFeatureLayer selectedLayer) {
                        if (selectedLayer == layer) {
                            return pair.Value;
                        }
                    }

                    return null;
                });
        }

        private static async Task<Dictionary<MapMember, List<long>>> GetSelection(Map map) {
            if (map.SelectionCount == 0) {
                return new Dictionary<MapMember, List<long>>();
            }

            return await ThreadService.RunOnBackground(map.GetSelection);
        }

        private static async void RemoveSelections(Map map) {
            var selection = await GetSelection(map);

            foreach (var pair in selection) {
                if (pair.Key is BasicFeatureLayer layer) {
                    await ThreadService.RunOnBackground(() => layer.ClearSelection());
                }
            }
        }

        public static async Task SetSelectionFromId(long id, Layer layer) => await
            ThreadService.RunOnBackground(() => layer.Map.SetSelection(new Dictionary<MapMember, List<long>> {
                {
                    layer, new List<long> {
                        id
                    }
                }
            }));
    }
}
