using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using Reactive.Bindings;

namespace uic_addin.Views {
    internal class EvergreenSettingsViewModel : Page {
        private bool _betaChannel;

        public EvergreenSettingsViewModel() {
            CurrentVersion = UicModule.Current.Evergreen.Select(x =>
                             UicModule.Current.EvergreenSettings.CurrentVersion.AddInVersion).ToReactiveProperty();

            OpenRepository.Subscribe(() => Process.Start("https://github.com/agrc/uic-addin"));
        }

        public ReactiveCommand OpenRepository { get; set; } = new ReactiveCommand();

        public bool BetaChannel {
            get => _betaChannel;
            set {
                var modified = value != _betaChannel;

                if (SetProperty(ref _betaChannel, value, () => BetaChannel) && modified) {
                    IsModified = true;
                }
            }
        }

        public ReactiveProperty<string> CurrentVersion { get; }

        /// <summary>
        ///     Invoked when the OK or apply button on the property sheet has been clicked.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        /// <remarks>This function is only called if the page has set its IsModified flag to true.</remarks>
        protected override async Task CommitAsync() {
            var settings = UicModule.Current.Settings;

            settings["UICAddin.Evergreen.BetaChannel"] = BetaChannel.ToString();

            if (BetaChannel != UicModule.Current.EvergreenSettings.BetaChannel) {
                Project.Current.SetDirty();
            }

            UicModule.Current.EvergreenSettings.BetaChannel = BetaChannel;

            try {
                await UicModule.Current.CheckForLastest();
            } catch {
                // ignored
            }
        }

        /// <summary>
        ///     Called when the page loads because to has become visible.
        /// </summary>
        /// <returns>A task that represents the work queued to execute in the ThreadPool.</returns>
        protected override Task InitializeAsync() {
            var useBetaChannel = false;
            var settings = UicModule.Current.Settings;

            if (settings.TryGetValue("UICAddin.Evergreen.BetaChannel", out var value)) {
                bool.TryParse(value, out useBetaChannel);
            }

            _betaChannel = useBetaChannel;

            return Task.FromResult(0);
        }
    }
}
