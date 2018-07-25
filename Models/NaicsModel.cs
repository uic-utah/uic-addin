using System;
using System.Collections.Generic;
using System.Linq;

namespace uic_addin.Models {
    public struct NaicsModel {

        public NaicsModel(double code, string title) {
            Code = Convert.ToInt32(code);
            Title = title;
        }

        public int Code { get; set; }
        public string Title { get; set; }


        public static IEnumerable<NaicsModel> CreateNaicsFromRange(string range, string title) {
            if (!range.Contains("-")) {
                return Enumerable.Empty<NaicsModel>();
            }

            var ranges = range.Split('-');
            var low = Convert.ToInt32(ranges[0]);
            var high = Convert.ToInt32(ranges[1]);

            return Enumerable.Range(low, (high - low) + 1).Select(x => new NaicsModel(x, title));
        }
    }
}
