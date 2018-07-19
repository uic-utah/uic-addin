using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;
using uic_addin.Views;

namespace uic_addin.Controls {
    internal class CreateNewFacilityPaneViewModel : ViewStatePane {
        private const string ViewPaneId = "CreateNewFacilityPane";
        private Guid FacilityGuid { get; set; }

        public ReactiveCommand CreateFacilityCommand { get; }

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public CreateNewFacilityPaneViewModel(CIMView view)
            : base(view) {

            CreateFacilityCommand = new ReactiveCommand();
            CreateFacilityCommand.Subscribe(async () => await CreateFacility());
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

            FacilityGuid = Guid.NewGuid();

            var guidString = FacilityGuid.ToString().ToUpper();
            return $"UTU{fips}F{guidString.Substring(guidString.Length - 8)}";
        }

        private async Task CreateFacility() {
            var facilityLayer = UicModule.Current.Layers[FacilityModel.TableName];

            var operation = new EditOperation {
                Name = "Create new Facility",
                SelectNewFeatures = true,
                ShowProgressor = true,
                ProgressMessage = "Creating new facility"
            };

            var attributes = new Dictionary<string, object> {
                ["GUID"] = FacilityGuid,
                ["FacilityId"] = NewFacilityId.Value,
                ["CountyFIPS"] = CountyFips.Value
            };

            operation.Create(facilityLayer, attributes);
            await operation.ExecuteAsync();

            if (operation.IsSucceeded) {
                var pane = UicModule.Current.DockPanes[WorkflowDockPaneViewModel.DockPaneId] as WorkflowDockPaneViewModel;
                pane?.ShowAttributeEditorForSelectedRecord.Execute(null);

                await UninitializeAsync();

                return;
            }

            MessageBox.Show(operation.ErrorMessage, "Create Facility Error");
        }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            await base.UninitializeAsync();
        }
    }
}
