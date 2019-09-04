using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace uic_addin.Services {
    public class QueryService {
        public static SortedList<object, string> GetDomainFor(FeatureClass layer, string fieldName) {
            var definition = layer.GetDefinition();

            var fieldIndex = definition.FindField(fieldName);
            var field = definition.GetFields()[fieldIndex];

            var domain = field.GetDomain() as CodedValueDomain;

            return domain?.GetCodedValuePairs();
        }

        //public static async Task<IEnumerable<string>> GetFacilityIdsFor(string id, Map map) => await QueuedTask.Run(() => {
        //    var layer = UicModule.Current.Layers[FacilityModel.TableName] as FeatureLayer;

        //    var filter = new QueryFilter {
        //        WhereClause = $"FacilityID LIKE '{id}%'"
        //    };

        //    var collection = new List<string>();

        //    using (var cursor = layer.Search(filter)) {
        //        while (cursor.MoveNext()) {
        //            collection.Add(cursor.Current["FacilityID"].ToString());
        //        }
        //    }

        //    return collection;
        //});
    }
}
