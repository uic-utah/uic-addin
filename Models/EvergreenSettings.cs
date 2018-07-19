using Octokit;
using ProEvergreen;

namespace uic_addin.Models {
    public class EvergreenSettings {
        public bool BetaChannel { get; set; }
        public VersionInformation CurrentVersion { get; set; }
        public Release LatestRelease { get; set; }
    }
}
