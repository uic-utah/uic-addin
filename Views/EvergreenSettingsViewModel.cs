using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using Reactive.Bindings;

namespace uic_addin.Views {
    internal class EvergreenSettingsViewModel : Page {
        private bool _originalBetaChannel = false;

        public EvergreenSettingsViewModel() {
            CurrentVersion = UicModule.Evergreen.Select(x => x.GetCurrentAddInVersion().AddInVersion)
                                      .ToReactiveProperty();

            OpenRepository = new ReactiveCommand();

            OpenRepository.Subscribe(() => Open("https://github.com/agrc/uic-addin"));
        }

        public ReactiveCommand OpenRepository { get; set; }

        private bool _betaChannel;
        private bool _dirty;

        public bool BetaChannel {
            get => _betaChannel;
            set {
                if (SetProperty(ref _betaChannel, value, () => BetaChannel))
                    IsModified = true;
            }
        }

        public ReactiveProperty<string> CurrentVersion { get; }

        public void Open(string url) => Process.Start(url);

        /// <summary>
        /// Invoked when the OK or apply button on the property sheet has been clicked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        /// <remarks>This function is only called if the page has set its IsModified flag to true.</remarks>
        protected override async Task CommitAsync() {
            var settings = UicModule.Current.Settings;

            settings["UICAddin.Evergreen.BetaChannel"] = BetaChannel.ToString();
            UicModule.EvergreenSettings.BetaChannel = BetaChannel;
            
            Project.Current.SetDirty();

            await UicModule.CheckForLastest();
        }

        /// <summary>
        /// Called when the page loads because to has become visible.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        protected override Task InitializeAsync() {
            var useBetaChannel = false;
            var settings = UicModule.Current.Settings;
            if (settings.TryGetValue("UICAddin.Evergreen.BetaChannel", out var value)) {
                bool.TryParse(value, out useBetaChannel);
            }

            BetaChannel = useBetaChannel;

            return Task.FromResult(0);
        }

        public bool Dirty => _originalBetaChannel != UicModule.EvergreenSettings.BetaChannel;

        /// <summary>
        /// Called when the page is destroyed.
        /// </summary>
        protected override void Uninitialize() {
        }
    }
}
