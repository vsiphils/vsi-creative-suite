using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
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
                string url = "https://github.com/vsiphils/vsi-creative-suite/releases/download/v0.3.2/VSI-Creative-Suite-Setup-Win10-11-x64.exe";
                UpdateRelease valid = SuiteUpdater.ParseRelease(Json("v0.3.2", digest, url, false));
                Check(valid != null, "Newer release metadata available");
                Check(valid.Version.ToString() == "0.3.2", "Version comparison");
                Check(valid.Size == 15000000, "Size read");
                Check(valid.Sha256 == hash, "Digest read");
                Check(SuiteUpdater.ParseRelease(Json("v0.3.2", digest, url, true)) == null,
                    "Pre-releases ignored");
                Check(SuiteUpdater.ParseRelease(Json("v0.3.0", digest, url, false)) == null,
                    "Current release not offered again");
                Check(SuiteUpdater.ParseRelease(Json("v0.2.9", digest, url, false)) == null,
                    "Downgrades ignored");
                MustReject(Json("v0.3.2", "sha256:invalid", url, false));
                MustReject(Json("v0.3.2", digest, "https://attacker.example/installer.exe", false));
                MustReject(Json("v0.3.2", digest,
                    url.Replace("/vsiphils/", "/other-owner/"), false));
                Check(SuiteUpdater.ParseRelease("{\"tag_name\":\"v0.3.2\",\"prerelease\":true,\"assets\":[]}") == null,
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

                string approvedJson = "{\n  \"schemaVersion\": 1,\n  \"suiteVersion\": \"0.3.1\",\n  \"channel\": \"stable\",\n  \"apps\": [\n    {\n      \"id\": \"hew\",\n      \"name\": \"Hew 3D\",\n      \"description\": \"3D Modeling\",\n      \"executable\": \"hew.exe\",\n      \"symbol\": \"3D\",\n      \"accent\": \"#E64124\",\n      \"delivery\": \"bundled-installer\",\n      \"repository\": \"hew3d/hew\",\n      \"assetNameTemplate\": \"Hew_{version}_x64-setup.exe\"\n    },\n    {\n      \"id\": \"pdfcraft\",\n      \"name\": \"PdfCraft\",\n      \"description\": \"PDF Tools\",\n      \"executable\": \"pdfcraft.exe\",\n      \"symbol\": \"PDF\",\n      \"accent\": \"#008F81\",\n      \"delivery\": \"bundled-portable\",\n      \"repository\": \"storytold/pdfcraft\",\n      \"assetNameTemplate\": \"pdfcraft-{version}-windows-x64-portable.zip\"\n    },\n    {\n      \"id\": \"photocraft\",\n      \"name\": \"PhotoCraft\",\n      \"description\": \"Image Editing\",\n      \"executable\": \"photocraft.exe\",\n      \"symbol\": \"P\",\n      \"accent\": \"#0075C3\",\n      \"delivery\": \"bundled-portable\",\n      \"repository\": \"storytold/photocraft\",\n      \"assetNameTemplate\": \"photocraft-{version}-windows-x64-portable.zip\"\n    },\n    {\n      \"id\": \"gridcraft\",\n      \"name\": \"GridCraft\",\n      \"description\": \"Spreadsheets\",\n      \"executable\": \"gridcraft.exe\",\n      \"symbol\": \"X\",\n      \"accent\": \"#2B9C4C\",\n      \"delivery\": \"bundled-portable\",\n      \"repository\": \"storytold/gridcraft\",\n      \"assetNameTemplate\": \"gridcraft-{version}-windows-x64-portable.zip\"\n    },\n    {\n      \"id\": \"wordcraft\",\n      \"name\": \"WordCraft\",\n      \"description\": \"Documents\",\n      \"executable\": \"wordcraft.exe\",\n      \"symbol\": \"W\",\n      \"accent\": \"#2967CD\",\n      \"delivery\": \"bundled-portable\",\n      \"repository\": \"storytold/wordcraft\",\n      \"assetNameTemplate\": \"wordcraft-{version}-windows-x64-portable.zip\"\n    },\n    {\n      \"id\": \"deckcraft\",\n      \"name\": \"DeckCraft\",\n      \"description\": \"Presentations and Slides\",\n      \"executable\": \"deckcraft.exe\",\n      \"symbol\": \"D\",\n      \"accent\": \"#F07B23\",\n      \"delivery\": \"release-zip\",\n      \"repository\": \"storytold/deckcraft\",\n      \"assetNameTemplate\": \"deckcraft-{version}-windows-x64-portable.zip\"\n    },\n    {\n      \"id\": \"cadcraft\",\n      \"name\": \"CADCraft\",\n      \"description\": \"CAD Drafting\",\n      \"executable\": \"cadcraft.exe\",\n      \"symbol\": \"CAD\",\n      \"accent\": \"#8548D2\",\n      \"delivery\": \"bundled-portable\",\n      \"repository\": \"storytold/cadcraft\",\n      \"assetNameTemplate\": \"cadcraft-{version}-windows-x64-portable.zip\"\n    }\n  ]\n}\n";
                List<ApprovedApp> approved = AppUpdates.ParseCatalog(approvedJson);
                Check(approved.Count == 7, "Six bundled apps and one optional DeckCraft catalog app");
                Check(approved[4].Id == "wordcraft" &&
                    approved[4].Repository == "storytold/wordcraft",
                    "WordCraft upstream approved");
                Check(approved[5].Id == "deckcraft" && approved[5].Delivery == "release-zip",
                    "DeckCraft is optional, not bundled by default");
                string deckRelease = "{\"tag_name\":\"v0.15.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"deckcraft-0.15.0-windows-x64-portable.zip\",\"browser_download_url\":\"https://github.com/storytold/deckcraft/releases/download/v0.15.0/deckcraft-0.15.0-windows-x64-portable.zip\",\"size\":103882044,\"digest\":\"sha256:23d0f19360ef4a65654863291c023952cf57739eb646429c3b545bf1ab4d1d92\"}]}";
                AppUpdate deckOffer = AppUpdates.ParseCandidate(approved[5], "not installed", deckRelease);
                Check(deckOffer != null && deckOffer.Name == "DeckCraft" &&
                    deckOffer.Executable == "deckcraft.exe" && deckOffer.Tag == "v0.15.0",
                    "Official DeckCraft x64 release creates an optional NEW APP offer");
                string latestWord = "{\"tag_name\":\"v0.15.1\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"wordcraft-0.15.1-windows-x64-portable.zip\",\"browser_download_url\":\"https://github.com/storytold/wordcraft/releases/download/v0.15.1/wordcraft-0.15.1-windows-x64-portable.zip\",\"size\":67890123,\"digest\":\"sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\"}]}";
                AppUpdate newerWord = AppUpdates.ParseCandidate(approved[4], "v0.15.0", latestWord);
                Check(newerWord != null && newerWord.Tag == "v0.15.1",
                    "Individual WordCraft update detected");
                Check(newerWord.Executable == "wordcraft.exe", "Expected exe comes from catalog");
                Check(AppUpdates.ParseCandidate(approved[4], "v0.15.1", latestWord) == null,
                    "Already current upstream app is not offered");
                Check(AppUpdates.ParseCandidate(approved[4], "v0.15.2", latestWord) == null,
                    "No app downgrades offered");
                Check(AppUpdates.ParseCandidate(approved[4], "not installed", latestWord) != null,
                    "Approved new app can be offered for optional installation");
                bool unsafeMetadata = false;
                try { AppUpdates.ParseCandidate(approved[4], "v0.15.0",
                    latestWord.Replace("https://github.com/", "https://untrusted.example/")); }
                catch (InvalidDataException) { unsafeMetadata = true; }
                Check(unsafeMetadata, "App release from unapproved origin blocked");
                unsafeMetadata = false;
                try { AppUpdates.ParseCandidate(approved[4], "v0.15.0",
                    latestWord.Replace("sha256:", "sha1:")); }
                catch (InvalidDataException) { unsafeMetadata = true; }
                Check(unsafeMetadata, "App release without SHA256 blocked");
                string tempDirectory = Path.Combine(Path.GetTempPath(), "vsi-updater-test-" +
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);
                try
                {
                    string archive = Path.Combine(tempDirectory, "untrusted.zip");
                    using (FileStream output = File.Create(archive))
                    using (ZipArchive zip = new ZipArchive(output, ZipArchiveMode.Create))
                        zip.CreateEntry("../escape.txt");
                    bool traversalBlocked = false;
                    try { AppUpdatesDialog.ExtractSafely(archive, Path.Combine(tempDirectory, "target")); }
                    catch (InvalidDataException) { traversalBlocked = true; }
                    Check(traversalBlocked, "Archive path traversal blocked");
                    Check(!File.Exists(Path.Combine(tempDirectory, "escape.txt")),
                        "Archive could not write outside target");
                }
                finally { Directory.Delete(tempDirectory, true); }

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
