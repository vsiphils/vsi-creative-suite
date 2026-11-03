using System;
using System.IO;
using System.Net;

namespace VsiCreativeSuite
{
    internal static class UpdaterTests
    {
        private static int assertions;
        private static void Check(bool value, string label)
        {
            assertions++;
            if (!value) throw new Exception("FAIL: " + label);
        }

        private static string Json(string tag, string digest, string url, bool prerelease)
        {
            return "{\"tag_name\":\"" + tag +
                "\",\"draft\":false,\"prerelease\":" + (prerelease ? "true" : "false") +
                ",\"assets\":[{\"name\":\"VSI-Creative-Suite-Setup-Win10-11-x64.exe\"," +
                "\"browser_download_url\":\"" + url + "\"," +
                "\"size\":15000000,\"digest\":\"" + digest + "\"}]}";
        }
        private static void MustReject(string json)
        {
            bool rejected = false;
            try { SuiteUpdater.ParseRelease(json); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Invalid release metadata must fail closed");
        }

        private static int Main()
        {
            try
            {
                Check(SuiteUpdater.IsNoReleaseStatus(HttpStatusCode.NotFound),
                    "GitHub 404 means there is no published latest release");
                Check(!SuiteUpdater.IsNoReleaseStatus(HttpStatusCode.Forbidden),
                    "HTTP 403 rate limit or permission error must not be hidden");
                Check(!SuiteUpdater.IsNoReleaseStatus(HttpStatusCode.InternalServerError),
                    "HTTP 500 must not be mistaken for an empty release list");
                string hash = new string('a', 64);
                string digest = "sha256:" + hash;
                string url = "https://github.com/vsiphils/vsi-creative-suite/releases/download/v0.3.1/VSI-Creative-Suite-Setup-Win10-11-x64.exe";
                UpdateRelease valid = SuiteUpdater.ParseRelease(Json("v0.3.1", digest, url, false));
                Check(valid != null, "Newer release metadata available");
                Check(valid.Version.ToString() == "0.3.1", "Version comparison");
                Check(valid.Size == 15000000, "Size read");
                Check(valid.Sha256 == hash, "Digest read");
                Check(SuiteUpdater.ParseRelease(Json("v0.3.1", digest, url, true)) == null,
                    "Pre-releases ignored");
                Check(SuiteUpdater.ParseRelease(Json("v0.3.0", digest, url, false)) == null,
                    "Current release not offered again");
                Check(SuiteUpdater.ParseRelease(Json("v0.2.9", digest, url, false)) == null,
                    "Downgrades ignored");
                MustReject(Json("v0.3.1", "sha256:invalid", url, false));
                MustReject(Json("v0.3.1", digest, "https://attacker.example/installer.exe", false));
                MustReject(Json("v0.3.1", digest,
                    url.Replace("/vsiphils/", "/other-owner/"), false));
                Check(SuiteUpdater.ParseRelease("{\"tag_name\":\"v0.3.1\",\"prerelease\":true,\"assets\":[]}") == null,
                    "No prerelease accepted");

                string sample = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(sample, "abc");
                    Check(!SuiteUpdater.MatchesSha256(sample, hash), "Wrong hash is rejected");
                    Check(SuiteUpdater.MatchesSha256(sample,
                        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"),
                        "Known SHA256 must match");
                }
                finally { File.Delete(sample); }

                Console.WriteLine("PASS: " + assertions + " updater checks");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
