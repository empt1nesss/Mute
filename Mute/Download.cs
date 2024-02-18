using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;

using PuppeteerSharp;
using System.Reflection;
using System.Reflection.Metadata;

using VideoLibrary;
using Mute;

namespace Mute
{
    static class DownloadManager
    {
        static public Tuple<string, double>[] TrackList { get; private set; }
        static public string[] Urls { get; private set; }

        static public void GetInfo(string query)
        {
            string url = $"https://www.youtube.com/results?search_query={query}";

            List<string> divs = new List<string>();

            var browser = Puppeteer.LaunchAsync(new LaunchOptions{
                Headless = false,
                IgnoreHTTPSErrors = true,
                IgnoredDefaultArgs = new string[] { "--disable-extensions"},
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            }).Result;
            try
            {
                int height = 750;
                int width = 1250;
                Page page = null;
                var pages = browser.PagesAsync().Result;
                if (pages.Count() > 0)
                {
                    page = (Page?)pages[0];
                }
                else
                {
                    page = (Page?)browser.NewPageAsync().Result;
                }
                ViewPortOptions viewPort = new ViewPortOptions()
                {
                    Height = height,
                    Width = width
                };

                page.SetViewportAsync(viewPort).Wait();
                NavigationOptions options = new NavigationOptions() { Timeout = 120000 };
                page.DefaultNavigationTimeout = 120000;
                page.GoToAsync(url, options).Wait(10000);

                var content = page.GetContentAsync();
                var all_videos = page.QuerySelectorAllAsync("ytd-video-renderer.ytd-item-section-renderer").Result;

                for (int i = 0; i < all_videos.Length; i++)
                {
                    string tempTitle = Convert.ToString(all_videos[i].QuerySelectorAsync("yt-formatted-string.ytd-video-renderer").Result.GetPropertyAsync("innerText").Result)[9..];
                    // var f = new Tuple<string, double>(tempTitle, 0);
                    // System.Console.WriteLine(f);
                    // TrackList = TrackList.Append(f).ToArray();
                    TrackList= TrackList.Append(new Tuple<string, double>(tempTitle, 0)).ToArray();
                    // TrackList.Append(new Tuple<string, double>(fileName, -1.0)).ToArray();
                    System.Console.WriteLine($"{i+1}) {tempTitle}"); 
                }
                // |SELECT TRACK NUMBER OR "BACK"| V15% ||:
                // long dlPage = 0;
                // System.Console.WriteLine(123);
                // TUI.ShowTUI(this.TrackList, TUI.PlayerMods.DOWNLOAD, ref dlPage, $"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length + 1, Console.WindowHeight - 3, true);
                // должен сохраниться массив ссылок с каждого ролика
            }
            finally
            {
                if (browser != null) browser.CloseAsync().Wait();
            }
        }

        static public void Download(string url)
        {
            try
            {
                var yt = YouTube.Default;
                var video = yt.GetVideo(url);
                // System.Console.WriteLine($"[i] Trying write {TrackSpace.MainDirectory + "\\" + video.FullName}...\n");
                File.WriteAllBytes(TrackSpace.MainDirectory + "\\" + video.FullName, video.GetBytes());
                // System.Console.WriteLine("[+] Successfully!\n");

                var inputFilePath = TrackSpace.MainDirectory + "\\" + video.FullName;
                var outputFilePath = TrackSpace.MainDirectory + "\\" + video.FullName.Replace("mp4", "mp3");
                var mp3out = "";

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = @"C:\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";
                startInfo.Arguments = $" -i \"{inputFilePath}\" -vn -f mp3 -ab 320k output \"{outputFilePath}\" -y";
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = true;
                var process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit();

                string[] files = System.IO.Directory.GetFiles(TrackSpace.MainDirectory , "*.mp4");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

              TrackSpace.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        } 
        static public void DownloadByIndex(long index)
        {
          TrackSpace.Update();
        }
    }
}