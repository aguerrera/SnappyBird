using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            doImageThing("http://www.guerrera.org");
            doImageThing("http://www.msn.com");
            doImageThing("http://www.bing.com");
            doImageThing("http://www.google.com");
        }

        static void doImageThing(string url)
        {
            var bmp = SnappyBird.WebsiteSnapshotCSharpCheat.WebSnapshot.GetThumbnail(url, 1000, 1000, 100, 100);
            bmp.Save(@"c:\staging\snappy_output\" + Guid.NewGuid().ToString("N") + ".png");
        }
    }
}
