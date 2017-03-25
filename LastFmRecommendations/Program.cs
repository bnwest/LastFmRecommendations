using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LastFmRecommendations
{
    //
    // Could hand roll our own classes that we would derserailize the lastfm XML into but ..
    // there is a much easier way.  Visual Studio provides a mechanish to turn XML into 
    // serialize/deserialize ready classes.  See LastFm.cs for details.
    //

    class Program
    {
        static IsolatedStorageFile UserApplicationDirectory;


        static XDocument createXDoc(Stream stream, string xmlFile)
        {
            //string getLovedTracksUri =
            //    @"http://ws.audioscrobbler.com/2.0/" +
            //    @"?method=user.getlovedtracks" +
            //    @"&user=bnwest" +
            //    @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";
            //XDocument xdoc = XDocument.Load(getLovedTracksUri);

            // LINQ to XML technique:
            XDocument xdoc = XDocument.Load(stream);

            XElement root = xdoc.Root;

            int numLovedTracks;
            numLovedTracks = root.Descendants("track").Count();

            //<track>
            //  <name>No Reason To Stay</name>
            //  <mbid></mbid>
            //  <url>https://www.last.fm/music/Joanne+Shaw+Taylor/_/No+Reason+To+Stay</url>
            //  <date uts="1489425111">13 Mar 2017, 17:11</date>
            //  <artist>
            //    <name>Joanne Shaw Taylor</name>
            //    <mbid>1b3779c0-7414-465a-b811-a84f07131b89</mbid>
            //    <url>https://www.last.fm/music/Joanne+Shaw+Taylor</url>
            //  </artist>
            //  <image size="small">https://lastfm-img2.akamaized.net/i/u/34s/51dac8d22dca485fa6bf482dad5fb5da.png</image>
            //  <image size="medium">https://lastfm-img2.akamaized.net/i/u/64s/51dac8d22dca485fa6bf482dad5fb5da.png</image>
            //  <image size="large">https://lastfm-img2.akamaized.net/i/u/174s/51dac8d22dca485fa6bf482dad5fb5da.png</image>
            //  <image size="extralarge">https://lastfm-img2.akamaized.net/i/u/300x300/51dac8d22dca485fa6bf482dad5fb5da.png</image>
            //  <streamable fulltrack="0">0</streamable>
            //</track>

#if DEBUG
            foreach ( XElement track in root.Descendants("track") )
            {
                string trackName = (String) track.Element("name") ?? "";
                string trackMBId = (string) track.Element("mbid") ?? "";
                XElement artist = track.Element("artist");
                string artistName = (string)artist.Element("name") ?? "";
                string artistMBId = (string)artist.Element("mbid") ?? "";

                Console.WriteLine($"Track({trackName},{trackMBId}) Artist({artistName},{artistMBId}).");
            }
#endif
            //
            // write out LastFm xml
            //

            // FileMode.Create => create a new file or truncate/overwrite an existing file
            // serializing during a file write => get the xml declaration you expect

            using ( IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(xmlFile, FileMode.Create, UserApplicationDirectory) )
            {
                using ( StreamWriter writer = new StreamWriter(isoStream) )
                {
                    xdoc.Save(isoStream);
                }
            }

            //Console.WriteLine(xdoc.ToString()); // creates a UTF-16 string for the XML with an explicit UTF-16 encoding

            return xdoc;
        }


        static void Main(string[] args)
        {
            //
            // Going to write the XML return from lastfm.com to an xml file located in the app's Isoated Storage.
            //
            // IsolatedStorage for user + application (aka assembly):
            // user\AppData\Local\IsolatedStorage\c4a3baxt.uf4\1zfwuzwr.zbj\Url.3q4wuvqyhepg5buzfu0rgckwjx3mkj3q\AssemFiles\ or some such
            //

            UserApplicationDirectory =
              IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            //
            // as an exercise query lastfm for a set of loved tracks.
            // populate an XDocument instance using the XML lastfm returns and
            // populate class instance(s) also using the XML lastfm retruns.
            //

            XDocument xdoc;

            LovedTracks.lfm lastFm;
            //LovedTracks.lfmLovedtracks lovedTracks;

            //
            // create XDocument (similar to W3C XDOM) given an uri.
            //

            string getLovedTracksUri =
                @"http://ws.audioscrobbler.com/2.0/" +
                @"?method=user.getlovedtracks" +
                @"&user=bnwest" +
                @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";

            using ( MemoryStream memoryStream = new MemoryStream() )
            {
                WebClient webClient = new WebClient();
                using ( Stream webStream = webClient.OpenRead(getLovedTracksUri) )
                {
                    webStream.CopyTo(memoryStream);
                }

                string localXmlFile = "lastfm.xml"; // will be created in the Isolate Storage directory
                memoryStream.Position = 0;
                xdoc = createXDoc(memoryStream, localXmlFile);

                //
                // populate class instance(s) with XML
                //

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(LovedTracks.lfm));
                memoryStream.Position = 0;
                lastFm = (LovedTracks.lfm) xmlSerializer.Deserialize(memoryStream);

                //WebRequest webRequest = WebRequest.Create(getLovedTracksUri);
                //using ( WebResponse webResponse = webRequest.GetResponse() )
                //{
                //    using ( Stream stream = webResponse.GetResponseStream() )
                //    {
                //        lastFm = (LovedTracks.lfm) xmlSerializer.Deserialize(stream);
                //        // or
                //        using ( StreamReader readStream = new StreamReader(stream, Encoding.UTF8) )
                //        {
                //            lastFm = (LovedTracks.lfm) xmlSerializer.Deserialize(readStream);
                //        }
                //    }
                //}
            }

            return;

        }
    }
}
