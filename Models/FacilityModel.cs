using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using uic_addin.Attributes;
using uic_addin.ViewModels;

namespace uic_addin.Models {
    public class FacilityModel : PropertyChangedWithValidation {
        public const string IdField = "FacilityID";
        public const string TableName = "UicFacility";
        public ObservableCollection<string> FipsDomainValues = new ObservableCollection<string>();

        private string _comments;
        private string _countyFips;
        private string _facilityAddress;
        private string _facilityCity;
        private string _facilityGuid;
        private string _facilityMilepost;
        private string _facilityName = "";
        private string _facilityState;
        private string _facilityZip;
        private string _naicsPrimary;
        private string _facilityId = "";

        public FacilityModel() {
            FipsDomain = new ReadOnlyObservableCollection<string>(FipsDomainValues);
        }

        [Required(AllowEmptyStrings = false)]
        [StringLength(14, ErrorMessage = "Facility Identifiers contain 14 characters")]
        [DisplayName("Facility Id")]
        public string FacilityId {
            get => _facilityId;
            set => SetProperty(ref _facilityId, value);
        }

        [Required]
        [DisplayName("Guid")]
        public string FacilityGuid {
            get => _facilityGuid;
            set => SetProperty(ref _facilityGuid, value);
        }

        [Required]
        [FipsCode]
        [DisplayName("County FIPS")]
        public string CountyFips {
            get => _countyFips;
            set => SetProperty(ref _countyFips, value);
        }

        [Required]
        [StringLength(6, ErrorMessage = "NIACS codes are 1-6 characters")]
        [DisplayName("NAICS Code")]
        public string NaicsPrimary {
            get => _naicsPrimary;
            set => SetProperty(ref _naicsPrimary, value);
        }

        [Required(AllowEmptyStrings = false)]
        [DisplayName("Facility Name")]
        public string FacilityName {
            get => _facilityName;
            set => SetProperty(ref _facilityName, value);
        }

        [Required(AllowEmptyStrings = false)]
        [DisplayName("Facility Address")]
        public string FacilityAddress {
            get => _facilityAddress;
            set => SetProperty(ref _facilityAddress, value);
        }

        [Required(AllowEmptyStrings = false)]
        [DisplayName("Facility City")]
        public string FacilityCity {
            get => _facilityCity;
            set => SetProperty(ref _facilityCity, value);
        }

        [Required]
        [DisplayName("Facility State")]
        public string FacilityState {
            get => _facilityState;
            set => SetProperty(ref _facilityState, value);
        }

        public string FacilityZip {
            get => _facilityZip;
            set => SetProperty(ref _facilityZip, value);
        }

        public string FacilityMilepost {
            get => _facilityMilepost;
            set => SetProperty(ref _facilityMilepost, value);
        }

        public string Comments {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        public long ObjectId { get; set; }

        public ReadOnlyObservableCollection<string> FipsDomain { get; set; }
    }
}
