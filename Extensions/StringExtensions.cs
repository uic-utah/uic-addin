using System.Linq;

namespace uic_addin.Extensions {
    public static class StringExtensions {
        public static string SplitAndTakeLast(this string x, char character) {
            if (!x.Contains(character)) {
                return x;
            }

            return x.Split(character).Last();
        }
    }
}
