using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;

namespace uic_addin.Controls {
    internal class CreateNewFacilityControlViewModel : CustomControl {
        public CreateNewFacilityControlViewModel() {
            CreateFacilityCommand.Subscribe(async () => await CreateFacility());
            NewFacilityId = CountyFips.Select(Generate)
                                      .CatchIgnore()
                                      .ToReactiveProperty();
        }

        private Guid FacilityGuid { get; set; }

        public ReactiveCommand CreateFacilityCommand { get; } = new ReactiveCommand();

        public ReactiveProperty<string> NewFacilityId { get; set; }

        public ReactiveProperty<string> CountyFips { get; set; } = new ReactiveProperty<string>();

        public ObservableCollection<string> FipsCodes { get; set; } = new ObservableCollection<string>(new List<string> {
            "1",
            "2",
            "3"
        });

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
                ["CountyFIPS"] = CountyFips.Value,
                ["FacilityState"] = "UT"
            };

            operation.Create(facilityLayer, attributes);
            await operation.ExecuteAsync();

            if (operation.IsSucceeded) {
                //                var pane = UicModule.Current.DockPanes[WorkflowDockPaneViewModel.DockPaneId] as WorkflowDockPaneViewModel;
                //                pane?.ShowAttributeEditorForSelectedRecord.Execute(null);

                return;
            }

            MessageBox.Show(operation.ErrorMessage, "Create Facility Error");
        }
    }
}
