using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Serilog;

namespace uic_addin.Services {
    public class QueryService {
        public static async Task<IEnumerable<string>> GetFacilityIdsFor(string id) => await QueuedTask.Run(() => {
            var layer = FindLayer("uicfacility", MapView.Active);

            var filter = new QueryFilter {
                WhereClause = $"FacilityID LIKE '{id}%'"
            };

            var collection = new List<string>();

            using (var cursor = layer.Search(filter)) {
                while (cursor.MoveNext()) {
                    collection.Add(cursor.Current["FacilityID"].ToString());
                }
            }

            return collection;
        });

        public static BasicFeatureLayer FindLayer(string layerName, MapView activeView) {
            Log.Verbose("finding feature layer {layer}", layerName);

            var layers = activeView.Map.GetLayersAsFlattenedList();

            return (BasicFeatureLayer)layers.FirstOrDefault(x => string.Equals(SplitLast(x.Name),
                                                                               SplitLast(layerName),
                                                                               StringComparison
                                                                                   .InvariantCultureIgnoreCase));
        }

        private static string SplitLast(string x) {
            if (!x.Contains('.')) {
                return x;
            }

            return x.Split('.').Last();
        }
    }
}
