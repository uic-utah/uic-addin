using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ExcelDataReader;
using Reactive.Bindings;
using uic_addin.Models;

namespace uic_addin.Views {
    internal class NaicsFinderPaneViewModel : ViewStatePane {
        private const string ViewPaneId = "NaicsFinderPane";
        private readonly string _allNaicsPath;
        private readonly string _twoDigitCodesPath;

        /// <summary>
        ///     Consume the passed in CIMView. Call the base constructor to wire up the CIMView.
        /// </summary>
        public NaicsFinderPaneViewModel(CIMView view)
            : base(view) {
            var documentsFolder = Path.Combine(AddinAssemblyLocation(), "NaicsDocuments");
            _twoDigitCodesPath = Path.Combine(documentsFolder, "2_6_digit_2017_Codes.xlsx");
            _allNaicsPath = Path.Combine(documentsFolder, "2017_NAICS_Index_File.xlsx");

            FilteredNaics = new ReadOnlyObservableCollection<NaicsModel>(NaicsModels);

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
                new KeyValuePair<object, string>(56,
                                                 "Administrative and Support and Waste Management and Remediation Services"),
                new KeyValuePair<object, string>(61, "Educational Services"),
                new KeyValuePair<object, string>(62, "Health Care and Social Assistance"),
                new KeyValuePair<object, string>(71, "Arts, Entertainment, and Recreation"),
                new KeyValuePair<object, string>(72, "Accommodation and Food Services"),
                new KeyValuePair<object, string>(81, "Other Services (except Public Administration)"),
                new KeyValuePair<object, string>(92, "Public Administration")
            };

            NaicsCategories = new ObservableCollection<KeyValuePair<object, string>>(categories);
        }

        public ReactiveProperty<int> CurrentCode { get; set; } = new ReactiveProperty<int>();

        public RelayCommand ShowCategory { get; set; } 

        public ObservableCollection<KeyValuePair<object, string>> NaicsCategories { get; set; }

        private ObservableCollection<NaicsModel> NaicsModels { get; } = new ObservableCollection<NaicsModel>();

        public ReadOnlyObservableCollection<NaicsModel> FilteredNaics { get; set; }

        public List<NaicsModel> NaicsCodes { get; set; } = new List<NaicsModel>();

        /// <summary>
        ///     Must be overridden in child classes used to persist the state of the view to the CIM.
        /// </summary>
        public override CIMView ViewState {
            get {
                _cimView.InstanceID = (int)InstanceID;
                return _cimView;
            }
        }

        public List<NaicsModel> AllNaicsCodes { get; set; } = new List<NaicsModel>();

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
        ///     Called when the pane is initialized.
        /// </summary>
        protected override async Task InitializeAsync() {
            await base.InitializeAsync();

            ShowCategory = new RelayCommand(SetActive, () => true);
            HasValue = CurrentCode.Select(x => x.ToString().Length == 6)
                .ToReactiveProperty();
            SetClipboard = HasValue.ToReactiveCommand()
                                   .WithSubscribe(() => Clipboard.SetText(CurrentCode.Value.ToString()));

            var codeColumn = 1;
            var titleColumn = 2;
            var skipRows = 2;
            var currentRow = 0;

            try {
                using (var stream = File.Open(_twoDigitCodesPath, FileMode.Open, FileAccess.Read)) {
                    using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                        do {
                            while (reader.Read()) {
                                currentRow += 1;
                                if (currentRow <= skipRows) {
                                    continue;
                                }

                                try {
                                    NaicsCodes.Add(new NaicsModel(reader.GetDouble(codeColumn),
                                                                  reader.GetString(titleColumn)));
                                } catch (InvalidCastException) {
                                    NaicsCodes.AddRange(NaicsModel.CreateNaicsFromRange(reader.GetString(codeColumn),
                                                                                        reader.GetString(titleColumn)));
                                }
                            }
                        } while (reader.NextResult());
                    }
                }
            } catch (Exception) {
            }

            currentRow = 0;
            skipRows = 1;
            codeColumn = 0;
            titleColumn = 1;

            try {
                using (var stream = File.Open(_allNaicsPath, FileMode.Open, FileAccess.Read)) {
                    using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                        do {
                            while (reader.Read()) {
                                currentRow += 1;
                                if (currentRow <= skipRows) {
                                    continue;
                                }

                                AllNaicsCodes.Add(new NaicsModel(reader.GetDouble(codeColumn),
                                                                 reader.GetString(titleColumn)));
                            }
                        } while (reader.NextResult());
                    }
                }
            } catch (Exception) {
            }
        }

        public ReactiveProperty<bool> HasValue { get; set; }

        public ReactiveCommand SetClipboard { get; set; }

        /// <summary>
        ///     Called when the pane is uninitialized.
        /// </summary>
        protected override async Task UninitializeAsync() {
            await base.UninitializeAsync();
        }

        public void SetActive(object item) {
            if (item == null || !int.TryParse(item.ToString(), out var code)) {
                return;
            }

            CurrentCode.Value = code;
            
            var start = code * 10;

            var depth = code.ToString().Length;
            var end = Convert.ToInt32(Math.Pow(10, depth)) - code + start;

            IEnumerable<NaicsModel> codes;
            if (depth == 6) {
                codes = AllNaicsCodes.Where(x => x.Code == code);
            } else {
                codes = NaicsCodes.Where(x => x.Code >= start && x.Code <= end);
            }

            NaicsModels.Clear();

            foreach (var model in codes) {
                NaicsModels.Add(model);
            }
        }

        private static string AddinAssemblyLocation() {
            var asm = Assembly.GetExecutingAssembly();

            return Path.GetDirectoryName(Uri.UnescapeDataString(new Uri(asm.CodeBase).LocalPath));
        }
    }
}
