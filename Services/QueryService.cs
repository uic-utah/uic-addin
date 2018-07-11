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
using uic_addin.Models;

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

        public static async Task<FacilityModel> GetFacilityFor(string id) => await QueuedTask.Run(() => {
            var layer = FindLayer("uicfacility", MapView.Active);

            var filter = new QueryFilter {
                WhereClause = $"FacilityID='{id}'"
            };

            var model = new FacilityModel(null);

            using (var cursor = layer.Search(filter)) {
                while (cursor.MoveNext()) {
                    var row = cursor.Current;

                    model.SelectedOid = Convert.ToInt64(row["OBJECTID"]);
                    model.UicFacilityId = Convert.ToString(row["FacilityID"]);
                    model.CountyFips = Convert.ToString(row["CountyFIPS"]);
                    model.NaicsPrimary = Convert.ToString(row["NAICSPrimary"]);
                    model.FacilityName = Convert.ToString(row["FacilityName"]);
                    model.FacilityAddress = Convert.ToString(row["FacilityAddress"]);
                    model.FacilityCity = Convert.ToString(row["FacilityCity"]);
                    model.FacilityState = Convert.ToString(row["FacilityState"]);
                    model.FacilityZip = Convert.ToString(row["FacilityZip"]);
                    model.FacilityMilepost = Convert.ToString(row["FacilityMilePost"]);
                    model.Comments = Convert.ToString(row["Comments"]);
                    model.FacilityGuid = Convert.ToString(row["GUID"]);
                }
            }

            return model;
        });

        public static async Task<IEnumerable<string>> GetFacilityIdsFor(IEnumerable<long> facilityObjectIds) => await
            QueuedTask.Run(() => {
                var layer = FindLayer("uicfacility", MapView.Active);

                var filter = new QueryFilter {
                    WhereClause = $"OBJECTID IN ({string.Join(",", facilityObjectIds)})"
                };

                var collection = new List<string>();

                using (var cursor = layer.Search(filter)) {
                    while (cursor.MoveNext()) {
                        collection.Add(cursor.Current["FacilityID"].ToString());
                    }
                }

                return collection;
            });
    }
}
