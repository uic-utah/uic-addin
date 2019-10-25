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
        private static Table filter(string name, IEnumerable<Table> l) {
            name = name.SplitAndTakeLast('.');
            return l.FirstOrDefault(table => {
                var tableName = table.GetName().SplitAndTakeLast('.');
                if (string.Equals(tableName, name, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                } else {
                    table.Dispose();

                    return false;
                }
            });
        }

        public static Layer GetLayer(string name, Map map) {
            Log.Verbose("finding feature layer {layer}", name);

            name = name.SplitAndTakeLast('.');

            var match = map.GetLayersAsFlattenedList().FirstOrDefault(layer => {
                using (var table = ((BasicFeatureLayer)layer).GetTable()) {
                    var currentTableName = table.GetName().SplitAndTakeLast('.');

                    Log.Verbose("checking {input} vs {current}", name, currentTableName);

                    if (string.Equals(currentTableName, name, StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }

                    return false;
                }
            });

            return match;
        }

        public static StandaloneTable GetStandaloneTable(string name, Map map) {
            Log.Verbose("finding stand alone table {layer}", name);

            name = name.SplitAndTakeLast('.');

            var match = map.StandaloneTables.FirstOrDefault(standAloneTable => {
                using (var table = standAloneTable.GetTable()) {
                    var currentTableName = table.GetName().SplitAndTakeLast('.');

                    Log.Verbose("checking {input} vs {current}", name, currentTableName);

                    if (string.Equals(currentTableName, name, StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }

                    return false;
                }
            });

            return match;
        }

        public static Table GetTableFromLayersOrTables(string layerName, Map map) {
            var layer = GetLayer(layerName, map);

            if (layer != null) {
                return ((BasicFeatureLayer)layer).GetTable();
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


        public static string GetDbSchema(Map map) {
            // assumes first layer is from active db
            var layer = map.GetLayersAsFlattenedList().FirstOrDefault(x => x.Name.ToLower().Contains("well"));

            if (layer is null) {
                NotificationService.NotifyOfMissingLayer("UICWell");

                throw new Exception("Could not find well layer. Please add it to your active map.");
            }

            using (var table = ((BasicFeatureLayer)layer).GetTable()) {
                var parts = table.GetName().Split('.');

                return $"{parts[0]}.{parts[1]}.";
            }
        }
    }
}
