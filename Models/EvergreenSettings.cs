using Octokit;

namespace uic_addin.Models {
    public class EvergreenSettings {
        public bool BetaChannel { get; set; }
        public Release LatestRelease { get; set; }
    }
}
