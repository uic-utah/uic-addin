using System;
using System.Collections;
using System.ComponentModel;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using Microsoft.Build.Framework;

namespace uic_addin.Models {
    public class FacilityModel :  PropertyChangedBase, INotifyDataErrorInfo {
        public const string IdField = "FacilityID";
        public const string TableName = "UicFacility";
        public readonly FeatureLayer FeatureLayer;

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
        private string _uicFacilityId = "";

        private FacilityModel() {
        }

        public FacilityModel(FeatureLayer facilityLayer) : this() {
            FeatureLayer = facilityLayer;
        }

        public long SelectedOid { get; set; }

        [Required]
        public string UicFacilityId {
            get => _uicFacilityId;
            set => SetProperty(ref _uicFacilityId, value);
        }

        [Required]
        public string FacilityGuid {
            get => _facilityGuid;
            set => SetProperty(ref _facilityGuid, value);
        }

        [Required]
        public string CountyFips {
            get => _countyFips;
            set => SetProperty(ref _countyFips, value);
        }

        [Required]
        public string NaicsPrimary {
            get => _naicsPrimary;
            set => SetProperty(ref _naicsPrimary, value);
        }

        [Required]
        public string FacilityName {
            get => _facilityName;
            set => SetProperty(ref _facilityName, value);
        }

        [Required]
        public string FacilityAddress {
            get => _facilityAddress;
            set => SetProperty(ref _facilityAddress, value);
        }

        [Required]
        public string FacilityCity {
            get => _facilityCity;
            set => SetProperty(ref _facilityCity, value);
        }

        [Required]
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

        public IEnumerable GetErrors(string propertyName) {
            throw new NotImplementedException();
        }

        public bool HasErrors { get; }
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}
