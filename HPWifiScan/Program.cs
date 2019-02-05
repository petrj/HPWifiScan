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
                //Console.WriteLine(postRequest);
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

            //Console.WriteLine($"Response XML:{responseString}");

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

                /*
                <scan:ScannerCapabilities xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:scan="http://schemas.hp.com/imaging/escl/2011/05/03" xmlns:pwg="http://www.pwg.org/schemas/2010/12/sm" xsi:schemaLocation="http://schemas.hp.com/imaging/escl/2011/05/03 eSCL.xsd">
                    <pwg:Version>2.63</pwg:Version>
                    <pwg:MakeAndModel>HP LaserJet MFP M28w</pwg:MakeAndModel>
                    <pwg:SerialNumber>VNC3K39835</pwg:SerialNumber>
                    <scan:UUID>564E4333-4B33-3938-3335-F43909F35BCE</scan:UUID>
                    <scan:AdminURI>http://NPIF35BCE.local.</scan:AdminURI>
                    <scan:IconURI>http://NPIF35BCE.local./ipp/images/printer.png</scan:IconURI>
                    <scan:Platen>
                        <scan:PlatenInputCaps>
                            <scan:MinWidth>300</scan:MinWidth>
                            <scan:MaxWidth>2550</scan:MaxWidth>
                            <scan:MinHeight>300</scan:MinHeight>
                            <scan:MaxHeight>3508</scan:MaxHeight>
                            <scan:MaxScanRegions>1</scan:MaxScanRegions>
                            <scan:SettingProfiles>
                                <scan:SettingProfile>
                                    <scan:ColorModes>
                                        <scan:ColorMode>RGB24</scan:ColorMode>
                                        <scan:ColorMode>Grayscale8</scan:ColorMode>
                                    </scan:ColorModes>
                                    <scan:ContentTypes>
                                        <pwg:ContentType>Photo</pwg:ContentType>
                                        <pwg:ContentType>Text</pwg:ContentType>
                                        <pwg:ContentType>TextAndPhoto</pwg:ContentType>
                                    </scan:ContentTypes>
                                    <scan:DocumentFormats>
                                        <pwg:DocumentFormat>image/jpeg</pwg:DocumentFormat>
                                        <pwg:DocumentFormat>application/pdf</pwg:DocumentFormat>
                                        <pwg:DocumentFormat>application/octet-stream</pwg:DocumentFormat>
                                        <scan:DocumentFormatExt>image/jpeg</scan:DocumentFormatExt>
                                        <scan:DocumentFormatExt>application/pdf</scan:DocumentFormatExt>
                                        <scan:DocumentFormatExt>application/octet-stream</scan:DocumentFormatExt>
                                    </scan:DocumentFormats>
                                    <scan:SupportedResolutions>
                                        <scan:DiscreteResolutions>
                                            <scan:DiscreteResolution>
                                                <scan:XResolution>200</scan:XResolution>
                                                <scan:YResolution>200</scan:YResolution>
                                            </scan:DiscreteResolution>
                                            <scan:DiscreteResolution>
                                                <scan:XResolution>300</scan:XResolution>
                                                <scan:YResolution>300</scan:YResolution>
                                            </scan:DiscreteResolution>
                                            <scan:DiscreteResolution>
                                                <scan:XResolution>600</scan:XResolution>
                                                <scan:YResolution>600</scan:YResolution>
                                            </scan:DiscreteResolution>
                                        </scan:DiscreteResolutions>
                                    </scan:SupportedResolutions>
                                    <scan:ColorSpaces>
                                        <scan:ColorSpace>sRGB</scan:ColorSpace>
                                    </scan:ColorSpaces>
                                </scan:SettingProfile>
                            </scan:SettingProfiles>
                            <scan:SupportedIntents>
                                <scan:Intent>Document</scan:Intent>
                                <scan:Intent>Photo</scan:Intent>
                                <scan:Intent>Preview</scan:Intent>
                                <scan:Intent>TextAndGraphic</scan:Intent>
                            </scan:SupportedIntents>
                            <scan:MaxOpticalXResolution>600</scan:MaxOpticalXResolution>
                            <scan:MaxOpticalYResolution>600</scan:MaxOpticalYResolution>
                        </scan:PlatenInputCaps>
                    </scan:Platen>
                    <scan:eSCLConfigCap>
                        <scan:StateSupport>
                            <scan:State>disabled</scan:State>
                            <scan:State>enabled</scan:State>
                        </scan:StateSupport>
                        <scan:ScannerAdminCredentialsSupport>true</scan:ScannerAdminCredentialsSupport>
                    </scan:eSCLConfigCap>
                </scan:ScannerCapabilities>
                */

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
                              <scan:ContentType>Photo</scan:ContentType>
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

                // parsing jobUri from Location
                var jobUri = responseLocation.Substring($"http://{printerUrl}:80".Length);

                var scannedDocumentUrl = responseLocation + "/NextDocument";

                Console.WriteLine($"Scanned data url: {scannedDocumentUrl}");

                // scanning ...

                Console.WriteLine("Waiting 5 secs ...");
                Thread.Sleep(5000);

                try
                {
                    // get scanner status

                    var xmlScannerStatusUrl = $"http://{printerUrl}:8080/eSCL/ScannerStatus";

                    bool scanIsCompleted = false;
                    int totalSeconds = 0;

                    do
                    {
                        var xmlScannerStatus = SendXMLGETRequest(xmlScannerStatusUrl);

                        var ns2 = new XmlNamespaceManager(xmlScannerStatus.NameTable);
                        ns2.AddNamespace("scan", "http://schemas.hp.com/imaging/escl/2011/05/03");
                        ns2.AddNamespace("pwg", "http://www.pwg.org/schemas/2010/12/sm");

                        /* scanner status example:
                         <scan:ScannerStatus xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:scan="http://schemas.hp.com/imaging/escl/2011/05/03" xmlns:pwg="http://www.pwg.org/schemas/2010/12/sm" xsi:schemaLocation="http://schemas.hp.com/imaging/escl/2011/05/03 eSCL.xsd">
                            <pwg:Version>2.63</pwg:Version>
                            <pwg:State>Idle</pwg:State>
                            <scan:Jobs>
                                <scan:JobInfo>
                                    <pwg:JobUri>/eSCL/ScanJobs/85kj8b1v-pgw1-059e-1024-1kg03nfw</pwg:JobUri>
                                    <pwg:JobUuid>85kj8b1v-pgw1-059e-1024-1kg03nfw</pwg:JobUuid>
                                    <scan:Age>287</scan:Age>
                                    <pwg:ImagesCompleted>1</pwg:ImagesCompleted>
                                    <pwg:ImagesToTransfer>1</pwg:ImagesToTransfer>
                                    <scan:TransferRetryCount>0</scan:TransferRetryCount>
                                    <pwg:JobState>Aborted</pwg:JobState>
                                    <pwg:JobStateReasons>
                                        <pwg:JobStateReason>JobCanceledAtDevice</pwg:JobStateReason>
                                    </pwg:JobStateReasons>
                                </scan:JobInfo>
                                <scan:JobInfo>
                                    <pwg:JobUri>/eSCL/ScanJobs/85kj8b1v-szc8-5ozl-1023-5or0hi8f</pwg:JobUri>
                                    <pwg:JobUuid>85kj8b1v-szc8-5ozl-1023-5or0hi8f</pwg:JobUuid>
                                    <scan:Age>374</scan:Age>
                                    <pwg:ImagesCompleted>1</pwg:ImagesCompleted>
                                    <pwg:ImagesToTransfer>1</pwg:ImagesToTransfer>
                                    <scan:TransferRetryCount>0</scan:TransferRetryCount>
                                    <pwg:JobState>Aborted</pwg:JobState>
                                    <pwg:JobStateReasons>
                                        <pwg:JobStateReason>JobCanceledAtDevice</pwg:JobStateReason>
                                    </pwg:JobStateReasons>
                                </scan:JobInfo>
                            </scan:Jobs>
                        </scan:ScannerStatus>
                        */

                        var scanJobs = xmlScannerStatus.SelectNodes("//scan:ScannerStatus/scan:Jobs/scan:JobInfo", ns2);

                        foreach (XmlNode jobInfoNode in scanJobs)
                        {
                            var scanJobUriNode = jobInfoNode.SelectSingleNode("pwg:JobUri", ns2);

                            if (scanJobUriNode.InnerText == jobUri)
                            {
                                XmlNode imagesToTransferNode = jobInfoNode.SelectSingleNode("pwg:ImagesToTransfer", ns2);
                                if (imagesToTransferNode.InnerText == "1")
                                {
                                    scanIsCompleted = true;
                                }
                                else
                                {
                                    Console.Write(".");
                                }
                            }
                        }

                        if (!scanIsCompleted)
                        {
                            Console.WriteLine("Waiting 2 secs ...");
                            Thread.Sleep(2000);

                            totalSeconds += 2;
                            if (totalSeconds > 120)
                            {
                                throw new Exception("Scan timeout after 120 seconds!");
                            }
                        }

                    } while (!scanIsCompleted);

                } catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine($"Error while getting scanner status, waiting 20 seconds for downloading {scannedDocumentUrl} to {fileName}");
                    Thread.Sleep(20 * 1000);
                }

                Console.WriteLine();

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
