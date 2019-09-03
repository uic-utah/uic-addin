using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;

namespace uic_addin.Services {
    public class QueryService {
        public static SortedList<object, string> GetDomainFor(FeatureClass layer, string fieldName) {
            var definition = layer.GetDefinition();

            var fieldIndex = definition.FindField(fieldName);
            var field = definition.GetFields()[fieldIndex];

            var domain = field.GetDomain() as CodedValueDomain;

            return domain?.GetCodedValuePairs();
        }
    }
}
