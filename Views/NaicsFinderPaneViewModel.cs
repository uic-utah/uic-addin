using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;

namespace uic_addin.Views {
    internal class NaicsFinderPaneViewModel : ViewStatePane {
        private const string ViewPaneId = "NaicsFinderPane";

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public NaicsFinderPaneViewModel(CIMView view)
            : base(view) {
           
        }

        public RelayCommand ShowCategory { get; set; }

        public ObservableCollection<KeyValuePair<object, string>> NaicsCategories { get; set; }

        /// <summary>
        ///     Create a new instance of the pane.
        /// </summary>
        internal static NaicsFinderPaneViewModel Create() {
            var view = new CIMGenericView {
                ViewType = ViewPaneId
            };
            return FrameworkApplication.Panes.Create(ViewPaneId, view) as NaicsFinderPaneViewModel;
        }

        /// <summary>
        ///     Must be overridden in child classes used to persist the state of the view to the CIM.
        /// </summary>
        public override CIMView ViewState {
            get {
                _cimView.InstanceID = (int)InstanceID;
                return _cimView;
            }
        }

        /// <summary>
        ///     Called when the pane is initialized.
        /// </summary>
        protected override async Task InitializeAsync() {
            var categories = new List<KeyValuePair<object, string>>(26) {
                new KeyValuePair<object, string>(11, "Agriculture, Forestry, Fishing and Hunting"),
                new KeyValuePair<object, string>(21, "Mining, Quarrying, and Oil and Gas Extraction"),
                new KeyValuePair<object, string>(22, "Utilities"),
                new KeyValuePair<object, string>(23, "Construction"),
                new KeyValuePair<object, string>("31-33", "Manufacturing"),
                new KeyValuePair<object, string>(42, "Wholesale Trade"),
                new KeyValuePair<object, string>("44-45", "Retail Trade"),
                new KeyValuePair<object, string>("48-49", "Transportation and Warehousing"),
                new KeyValuePair<object, string>(51, "Information"),
                new KeyValuePair<object, string>(52, "Finance and Insurance"),
                new KeyValuePair<object, string>(53, "Real Estate and Rental and Leasing"),
                new KeyValuePair<object, string>(54, "Professional, Scientific, and Technical Services"),
                new KeyValuePair<object, string>(55, "Management of Companies and Enterprises"),
                new KeyValuePair<object, string>(56, "Administrative and Support and Waste Management and Remediation Services"),
                new KeyValuePair<object, string>(61, "Educational Services"),
                new KeyValuePair<object, string>(62, "Health Care and Social Assistance"),
                new KeyValuePair<object, string>(71, "Arts, Entertainment, and Recreation"),
                new KeyValuePair<object, string>(72, "Accommodation and Food Services"),
                new KeyValuePair<object, string>(81, "Other Services (except Public Administration)"),
                new KeyValuePair<object, string>(92, "Public Administration")
            };

            NaicsCategories = new ObservableCollection<KeyValuePair<object, string>>(categories);

            ShowCategory = new RelayCommand(SetActive, () => true);

            await base.InitializeAsync();
        }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            await base.UninitializeAsync();
        }

        public void SetActive(object item) {

        }
    }
}
