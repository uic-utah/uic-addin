using System;
using System.ComponentModel.DataAnnotations;

namespace uic_addin.Attributes
{
    public sealed class FipsCodeAttribute : ValidationAttribute {
        public override string FormatErrorMessage(string name) => "County fips are odd numbers between 49001 and 49057";

        public override bool IsValid(object value) {
            try {
                var fips = Convert.ToInt32(value);
                if (fips % 2 == 0) {
                    // fips codes are all odd
                    return false;
                }

                return fips >= 49001 && fips <= 49057;
            } catch (Exception) {
                return false;
            }
        }
    }
}
