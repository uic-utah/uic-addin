using System;
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
        public static async Task<IEnumerable<string>> GetFacilityIdsFor(string id, Map map) => await QueuedTask.Run(() => {
            var layer = FindLayer("uicfacility", map ?? MapView.Active.Map);

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

        public static BasicFeatureLayer FindLayer(string layerName, Map map) {
            Log.Verbose("finding feature layer {layer}", layerName);

            var layers = map.GetLayersAsFlattenedList();

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

        public static async Task<FacilityModel> GetFacilityFor(string id, Map map=null) => await QueuedTask.Run(() => {
            var layer = FindLayer("uicfacility", map ?? MapView.Active.Map);

            var filter = new QueryFilter {
                WhereClause = $"FacilityID='{id}'"
            };

            var model = new FacilityModel(layer as FeatureLayer);

            using (var cursor = layer.Search(filter)) {
                while (cursor.MoveNext()) {
                    var row = cursor.Current;

                    model.SelectedOid = Convert.ToInt64(row["OBJECTID"]);
                    model.FacilityId = Convert.ToString(row["FacilityID"]);
                    model.CountyFips = Convert.ToString(row["CountyFIPS"]);
                    GetDomainFor(layer.GetTable() as FeatureClass, "CountyFIPS").ForEach(model.FipsDomainValues.Add);
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

        private static List<string> GetDomainFor(FeatureClass layer, string fieldName) {
            var definition = layer.GetDefinition();

            var fieldIndex = definition.FindField(fieldName);
            var field = definition.GetFields()[fieldIndex];

            var domain = field.GetDomain() as CodedValueDomain;

            return domain?.GetCodedValuePairs().Select(x => x.Value).ToList();
        }

        public static async Task<IEnumerable<string>> GetFacilityIdsFor(IEnumerable<long> facilityObjectIds, Map map=null) => await
            QueuedTask.Run(() => {
                var layer = FindLayer("uicfacility", map ?? MapView.Active.Map);

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
