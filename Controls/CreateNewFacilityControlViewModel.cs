using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Internal.Framework.Utilities;
using ArcGIS.Desktop.Mapping;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using uic_addin.Models;
using uic_addin.Services;

namespace uic_addin.Controls {
    internal class CreateNewFacilityControlViewModel : CustomControl {
        public CreateNewFacilityControlViewModel() {
            CreateFacilityCommand.Subscribe(async () => await CreateFacility());
            NewFacilityId = CountyFips.Select(Generate)
                                      .Select(SetButtonStatus)
                                      .CatchIgnore()
                                      .ToReactiveProperty();

            ThreadService.RunOnBackground(PopulateFips);
        }

        private Guid FacilityGuid { get; set; }

        public ReactiveCommand CreateFacilityCommand { get; } = new ReactiveCommand();

        public ReactiveProperty<string> NewFacilityId { get; set; }
        public ReactiveProperty<bool> EnableCreateButton { get; set; } = new ReactiveProperty<bool>(true);

        public ReactiveProperty<KeyValuePair<object, string>> CountyFips { get; set; } =
            new ReactiveProperty<KeyValuePair<object, string>>();

        public SortableObservableCollection<KeyValuePair<object, string>> FipsCodes { get; set; } =
            new SortableObservableCollection<KeyValuePair<object, string>>();

        private string Generate(KeyValuePair<object, string> pair) {
            if (pair.Key == null) {
                return null;
            }

            FacilityGuid = Guid.NewGuid();

            var guidString = FacilityGuid.ToString().ToUpper();

            var fips = pair.Key.ToString();
            var code = fips.Substring(fips.Length - 2);

            return $"UTU{code}F{guidString.Substring(guidString.Length - 8)}";
        }

        private string SetButtonStatus(string value) {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(NewFacilityId?.Value)) {
                EnableCreateButton.Value = true;
            } else {
                EnableCreateButton.Value = value != NewFacilityId.Value;
            }

            return value;
        }

        private void PopulateFips() {
            var fc = UicModule.Current.Layers[FacilityModel.TableName] as FeatureLayer;
            var domains = QueryService.GetDomainFor(fc.GetFeatureClass(), "CountyFIPS");

            foreach (var pair in domains) {
                FipsCodes.Add(pair);
            }
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
                var edits = FrameworkApplication.DockPaneManager.Find("esri_editing_AttributesDockPane");
//                edits.Pin();
//                edits.Activate(true);

                return;
            }

            MessageBox.Show(operation.ErrorMessage, "Create Facility Error");
            EnableCreateButton.Value = false;
        }
    }
}
