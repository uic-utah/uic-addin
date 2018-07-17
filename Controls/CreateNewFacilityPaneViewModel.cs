using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace uic_addin.Controls {
    internal class CreateNewFacilityPaneViewModel : ViewStatePane {
        private const string ViewPaneId = "CreateNewFacilityPane";

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public CreateNewFacilityPaneViewModel(CIMView view)
            : base(view) {
        }

        /// <summary>
        ///     Create a new instance of the pane.
        /// </summary>
        internal static CreateNewFacilityPaneViewModel Create() {
            var view = new CIMGenericView {
                ViewType = ViewPaneId
            };
            return FrameworkApplication.Panes.Create(ViewPaneId, view) as CreateNewFacilityPaneViewModel;
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
            CountyFips = new ReactiveProperty<string>();
            NewFacilityId = CountyFips.Select(Generate)
                                      .CatchIgnore()
                                      .ToReactiveProperty();

            await base.InitializeAsync();
        }

        public ReactiveProperty<string> NewFacilityId { get; set; }

        public ReactiveProperty<string> CountyFips { get; set; }

        public ObservableCollection<string> FipsCodes { get; set; } = new ObservableCollection<string>(new List<string> {"1", "2", "3"});

        private string Generate(string fips) {
            if (string.IsNullOrEmpty(fips)) {
                return null;
            }

            return $"UTU{fips}F-last8";
        }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            await base.UninitializeAsync();
        }
    }
}
