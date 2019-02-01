using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;


namespace HPWifiScan
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Missing arguments!");
                Help();
                return;
            }

            if (args.Length > 2)
            {
                Console.WriteLine("Too many arguments!");
                Help();
                return;
            }

            var ip = args[0];
            string fileName = "scan.jpg";

            if (args.Length == 2)
            {
                fileName = args[1];
            }

            DoScan(ip, 20, fileName);
        }

        public static void Help()
        {
            Console.WriteLine("HPWifiScan.exe");
            Console.WriteLine("");
            Console.WriteLine("Command line scanning tool for HP network printers");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("");
            Console.WriteLine("HPWifiScan.exe PrinterIP [filename]");
            Console.WriteLine("");
            Console.WriteLine("  default fileName is scan.jpg");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14 MyScannedFile.jpg");
        }

        private static HttpWebResponse SendRequest(string url, string method, string postRequest = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method;
            request.ContentType = "text/xml";
            request.ContentLength = postRequest == null ? 0 : postRequest.Length;

            Console.WriteLine($"Sending request to {url}");

            if (postRequest != null)
            {
                request.ContentType = "text/xml";

                var postData = Encoding.ASCII.GetBytes(postRequest);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postRequest.Length);
                }
                Console.WriteLine(postRequest);
            }

            return (HttpWebResponse)request.GetResponse();
        }

        private static XmlDocument SendXMLGETRequest(string url)
        {
            string responseString;
            using (var response = SendRequest(url, "GET"))
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    responseString = sr.ReadToEnd();
                }
            }

            var xml = new XmlDocument();
            xml.LoadXml(responseString);

            Console.WriteLine($"Response XML:{responseString}");

            return xml;
        }

        private static HttpWebResponse SendXMLPOSTRequest(string url, string xml)
        {
            return SendRequest(url, "POST", xml);
        }

        public static void DoScan(string printerUrl, int timeoutSeconds = 20, string fileName = "scan.jpg")
        {
            try
            {
                // get scanner capabilities
                var xmlCapabilitiesUrl = $"http://{printerUrl}:8080/eSCL/ScannerCapabilities";
                var xmlCapabilities = SendXMLGETRequest(xmlCapabilitiesUrl);

                var ns = new XmlNamespaceManager(xmlCapabilities.NameTable);
                ns.AddNamespace("scan", "http://schemas.hp.com/imaging/escl/2011/05/03");

                var maxWidth = xmlCapabilities.SelectSingleNode("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:MaxWidth", ns).InnerText;
                var maxHeight = xmlCapabilities.SelectSingleNode("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:MaxHeight", ns).InnerText;

                var scanXResolutionslist = new List<string>();
                var scanYResolutionslist = new List<string>();
                foreach (var node in xmlCapabilities.SelectNodes("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:SettingProfiles/scan:SettingProfile/scan:SupportedResolutions/scan:DiscreteResolutions/scan:DiscreteResolution/scan:XResolution", ns))
                {
                    scanXResolutionslist.Add((node as XmlNode).InnerText);
                }
                foreach (var node in xmlCapabilities.SelectNodes("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:SettingProfiles/scan:SettingProfile/scan:SupportedResolutions/scan:DiscreteResolutions/scan:DiscreteResolution/scan:YResolution", ns))
                {
                    scanYResolutionslist.Add((node as XmlNode).InnerText);
                }
                scanXResolutionslist.Sort();
                scanYResolutionslist.Sort();

                var maxXScanRes = scanXResolutionslist[scanXResolutionslist.Count - 1];
                var maxYScanRes = scanYResolutionslist[scanYResolutionslist.Count - 1];

                // sending scan request       

                var scanRequest = $@"<?xml version='1.0' encoding='UTF-8'?>
<scan:ScanSettings xmlns:pwg=""http://www.pwg.org/schemas/2010/12/sm"" xmlns:scan=""http://schemas.hp.com/imaging/escl/2011/05/03"">
  <pwg:Version>2.6</pwg:Version>
  <pwg:ScanRegions>
    <pwg:ScanRegion>
      <pwg:XOffset>0</pwg:XOffset>
      <pwg:YOffset>0</pwg:YOffset>
      <pwg:Width>{maxWidth}</pwg:Width>
      <pwg:Height>{maxHeight}</pwg:Height>
      <pwg:ContentRegionUnits>escl:ThreeHundredthsOfInches</pwg:ContentRegionUnits>
    </pwg:ScanRegion>
  </pwg:ScanRegions>
  <scan:InputSource>Platen</scan:InputSource>
  <scan:ColorMode>RGB24</scan:ColorMode>
  <scan:XResolution>{maxXScanRes}</scan:XResolution>
  <scan:YResolution>{maxYScanRes}</scan:YResolution>
</scan:ScanSettings>
";

                var scanRequestUrl = $"http://{printerUrl}:8080/eSCL/ScanJobs";

                string responseLocation = null;

                using (var response = SendXMLPOSTRequest(scanRequestUrl, scanRequest))
                {
                    foreach (var headerKey in response.Headers.AllKeys)
                    {
                        if (headerKey == "Location")
                        {
                            responseLocation = response.Headers[headerKey];
                        }
                    }
                }

                var scannedDocumentUrl = responseLocation + "/NextDocument";

                Console.WriteLine($"Scanned data url: {scannedDocumentUrl}");

                // scanning ...

                Console.WriteLine($"Waiting {timeoutSeconds} seconds ...");
                Thread.Sleep(timeoutSeconds * 1000);

                // saving scanned data  ...

                using (var response = SendRequest(scannedDocumentUrl, "GET"))
                {
                    using (Stream output = File.OpenWrite(fileName))
                    using (Stream input = response.GetResponseStream())
                    {
                        input.CopyTo(output);
                    }
                }

                Console.WriteLine($"Scanned data saved to {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error!");
                Console.WriteLine(ex);
            }
        }
    }
}
