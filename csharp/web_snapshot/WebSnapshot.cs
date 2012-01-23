using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace SnappyBird.WebsiteSnapshotCSharpCheat
{
    // this class gets a bmp from a website.
    // it is derived from this:
    // http://www.blackbeltcoder.com/Articles/graphics/creating-website-thumbnails-in-asp-net

    public class WebSnapshot
    {

        public string Url { get; set; }
        public int BrowserWidth { get; set; }
        public int BrowserHeight { get; set; }
        public int SnapHeight { get; set; }
        public int SnapWidth { get; set; }

        Bitmap _bmp;

        /// <summary>
        /// Generates a website thumbnail for the given URL
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="browserWidth">Browser width</param>
        /// <param name="browserHeight">Browser height</param>
        /// <param name="snapWidth">Width of generated snapshot</param>
        /// <param name="snapHeight">Height of generated snapshot</param>
        /// <returns></returns>
        public static Bitmap GetThumbnail(string url, int browserWidth, int browserHeight, int snapWidth, int snapHeight)
        {
            WebSnapshot snapshot = new WebSnapshot
            {
                Url = url,
                BrowserWidth = browserWidth,
                BrowserHeight = browserHeight,
                SnapWidth = snapWidth,
                SnapHeight = snapHeight
            };
            return snapshot.GetThumbnail();
        }

        /// <summary>
        /// Returns a thumbnail for the current member values
        /// </summary>
        /// <returns>Thumbnail bitmap</returns>
        protected Bitmap GetThumbnail()
        {
            // WebBrowser is an ActiveX control that must be run in a
            // single-threaded apartment so create a thread to create the
            // control and generate the thumbnail
            Thread thread = new Thread(new ThreadStart(GetThumbnailWorker));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return _bmp.GetThumbnailImage(SnapWidth, SnapHeight, null, IntPtr.Zero) as Bitmap;
        }

        /// <summary>
        /// Creates a WebBrowser control to generate the thumbnail image
        /// Must be called from a single-threaded apartment
        /// </summary>
        protected void GetThumbnailWorker()
        {
            using (WebBrowser browser = new WebBrowser())
            {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                browser.ClientSize = new Size(BrowserWidth, BrowserHeight);
                browser.ScrollBarsEnabled = false;
                browser.ScriptErrorsSuppressed = true;
                browser.Navigate(Url);
                //browser.AllowNavigation = false;
                // Wait for control to load page
                int counter = 0;
                while (browser.ReadyState != WebBrowserReadyState.Complete)
                {
                    Application.DoEvents();
                    if (browser.ReadyState == WebBrowserReadyState.Interactive || browser.ReadyState == WebBrowserReadyState.Loaded)
                    {
                        counter++;
                    }
                    if (stopWatch.Elapsed > TimeSpan.FromSeconds(4))
                    {
                        break;
                    }
                    if (counter == 100000)
                    {
                        break;
                    }
                }
                stopWatch.Stop();
                // Render browser content to bitmap
                _bmp = new Bitmap(BrowserWidth, BrowserHeight);
                browser.DrawToBitmap(_bmp, new Rectangle(0, 0, BrowserWidth, BrowserHeight));
                //browser.AllowNavigation = true;
            }
        }


    }
}
