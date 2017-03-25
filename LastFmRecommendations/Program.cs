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


        static XDocument createMostLovedTracksXDoc(Stream stream, string xmlFile)
        {
            //string getLovedTracksUri =
            //    @"http://ws.audioscrobbler.com/2.0/" +
            //    @"?method=user.getlovedtracks" +
            //    @"&user=bnwest" +
            //    @"&limit=10" +
            //    @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";
            //XDocument xdoc = XDocument.Load(getLovedTracksUri);

            // LINQ to XML technique:
            XDocument xdoc = XDocument.Load(stream);

            XElement root = xdoc.Root;

            int numLovedTracks;
            numLovedTracks = root.Descendants("track").Count();

            //foreach ( XElement track in root.Descendants("track") )
            //{
            //    string trackName = (String) track.Element("name") ?? "";
            //    string trackMBId = (string) track.Element("mbid") ?? "";
            //    XElement artist = track.Element("artist");
            //    string artistName = (string)artist.Element("name") ?? "";
            //    string artistMBId = (string)artist.Element("mbid") ?? "";
            //
            //    Console.WriteLine($"Track({trackName},{trackMBId}) Artist({artistName},{artistMBId}).");
            //}
   
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


        static XDocument createSimilarTracksXDoc(Stream stream, string xmlFile)
        {
            //string getSimilarTracksUri =
            //    @"http://ws.audioscrobbler.com/2.0/" +
            //    @"?method=track.getsimilar" +
            //    @"&artist=Deaf+Havana" +
            //    @"&track=Trigger" +
            //    @"&limit=5" +
            //    @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";
            //XDocument xdoc = XDocument.Load(getLovedTracksUri);

            // LINQ to XML technique:
            XDocument xdoc = XDocument.Load(stream);

            // FileMode.Append => append to an existing file
            // serializing during a file write => get the xml declaration you expect

            using ( IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(xmlFile, FileMode.Append, UserApplicationDirectory) )
            {
                using ( StreamWriter writer = new StreamWriter(isoStream) )
                {
                    xdoc.Save(isoStream);
                }
            }

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

            string localXmlFile = "lastfm.xml"; // will be created in the Isolate Storage directory

            //
            // as an exercise, query lastfm for a set of loved tracks.
            // populate an XDocument instance using the XML lastfm returns and
            // populate class instance(s) also using the XML lastfm retruns.
            //

            XDocument xdocLoved;

            LovedTracks.lfm lovedLastFm;

            //
            // create XDocument (similar to W3C XDOM) given an uri.
            //

            using ( MemoryStream memoryStream = new MemoryStream() )
            {
                string getLovedTracksUri =
                    @"http://ws.audioscrobbler.com/2.0/" +
                    @"?method=user.getlovedtracks" +
                    @"&user=bnwest" +
                    @"&limit=50" +
                    @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";

                WebClient webClient = new WebClient();
                using ( Stream webStream = webClient.OpenRead(getLovedTracksUri) )
                {
                    webStream.CopyTo(memoryStream);
                }

                memoryStream.Position = 0;
                xdocLoved = createMostLovedTracksXDoc(memoryStream, localXmlFile);

                //
                // populate class instance(s) with XML
                //

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(LovedTracks.lfm));
                memoryStream.Position = 0;
                lovedLastFm = (LovedTracks.lfm) xmlSerializer.Deserialize(memoryStream);

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

            //
            // For each of the loved tracks, get 5 recommendations
            //

            foreach ( var lovedTrack in lovedLastFm.lovedtracks.track )
            {
                string getSimilarTracksUri;

                SimilarTracks.lfm similarLastFm;

                XDocument xdocSimilar;

                if ( String.IsNullOrEmpty(lovedTrack.mbid) )
                {
                    getSimilarTracksUri =
                        @"http://ws.audioscrobbler.com/2.0/" +
                        @"?method=track.getsimilar" +
                        @"&artist=" + lovedTrack.artist.name.Replace(' ', '+') +
                        @"&track=" + lovedTrack.name.Replace(' ', '+') +
                        @"&limit=5" +
                        @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";

                }
                else
                {
                    getSimilarTracksUri =
                        @"http://ws.audioscrobbler.com/2.0/" +
                        @"?method=track.getsimilar" +
                        @"&mbid=" + lovedTrack.mbid +
                        @"&limit=5" +
                        @"&api_key=4eb95b1467e9cd940bd01b452a07dc98";
                }

                using ( MemoryStream memoryStream = new MemoryStream() )
                {
                    WebClient webClient = new WebClient();
                    using ( Stream webStream = webClient.OpenRead(getSimilarTracksUri) )
                    {
                        webStream.CopyTo(memoryStream);
                    }

                    memoryStream.Position = 0;
                    xdocSimilar = createSimilarTracksXDoc(memoryStream, localXmlFile);

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SimilarTracks.lfm));
                    memoryStream.Position = 0;
                    similarLastFm = (SimilarTracks.lfm) xmlSerializer.Deserialize(memoryStream);

                    Console.WriteLine($"Loved Track: Artist({lovedTrack.artist.name}) Track({lovedTrack.name})");
                    Console.WriteLine("Recommended:");

                    if ( similarLastFm.similartracks.track != null )
                    {
                        foreach ( SimilarTracks.lfmSimilartracksTrack similarTrack in similarLastFm.similartracks.track )
                        {
                            Console.WriteLine($"\tArtist({similarTrack.artist.name}) Track({similarTrack.name})");
                        }
                    }

                    Console.WriteLine();
                }
            }

            return;
        }
    }
}
