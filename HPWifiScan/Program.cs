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

            if (args.Length > 3)
            {
                Console.WriteLine("Too many arguments!");
                Help();
                return;
            }

            var ip = args[0];
            string fileName = "scan.jpg";

            if (args.Length > 1)
            {
                fileName = args[1];
            }

            var dpi = -1;

            if (args.Length == 3)
            {
                dpi = Convert.ToInt32(args[2]);
            }

            ServicePointManager.ServerCertificateValidationCallback += 
                (sender, cert, chain, sslPolicyErrors) => true;

            DoScan(ip, 20, fileName, dpi);
        }

        public static void Help()
        {
            Console.WriteLine("HPWifiScan.exe");
            Console.WriteLine("");
            Console.WriteLine("Command line scanning tool for HP network printers");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("");
            Console.WriteLine("HPWifiScan.exe PrinterIP [filename] [dpi]");
            Console.WriteLine("");
            Console.WriteLine("  default fileName is scan.jpg");
            Console.WriteLine("  default DPI maximum supported by printer");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14 MyScannedFile.jpg");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14 MyScannedFile-300DPI.jpg 300");
            Console.WriteLine("HPWifiScan.exe 10.0.0.14 MyScannedFile-600DPI.jpg 600");
        }

        private static HttpWebResponse SendRequest(string url, string method, int timeoutSeconds, string postRequest = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method;
            request.ContentType = "text/xml";
            request.ContentLength = postRequest == null ? 0 : postRequest.Length;
            request.Timeout = 20*1000; // 20s timeout

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

        private static XmlDocument SendXMLGETRequest(string url, int timeoutSeconds)
        {
            string responseString;
            using (var response = SendRequest(url, "GET", timeoutSeconds))
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

        private static HttpWebResponse SendXMLPOSTRequest(string url, string xml, int timeoutSeconds)
        {
            return SendRequest(url, "POST", timeoutSeconds, xml);
        }

        public static void DoScan(string printerUrl, int timeoutSeconds = 20, string fileName = "scan.jpg", int dpi=-1)
        {
            try
            {
                if (!printerUrl.StartsWith("http"))
                {
                    printerUrl = "https://" + printerUrl;
                }

                // get scanner capabilities
                var xmlCapabilitiesUrl = $"{printerUrl}:8080/eSCL/ScannerCapabilities";

                #region XML examples

                /* HP LaserJet MFP M28w :
                <scan:ScannerCapabilities xmlns:xsi="https://www.w3.org/2001/XMLSchema-instance" xmlns:scan="https://schemas.hp.com/imaging/escl/2011/05/03" xmlns:pwg="https://www.pwg.org/schemas/2010/12/sm" xsi:schemaLocation="https://schemas.hp.com/imaging/escl/2011/05/03 eSCL.xsd">
                    <pwg:Version>2.63</pwg:Version>
                    <pwg:MakeAndModel>HP LaserJet MFP M28w</pwg:MakeAndModel>
                    <pwg:SerialNumber>VNC3K39835</pwg:SerialNumber>
                    <scan:UUID>564E4333-4B33-3938-3335-F43909F35BCE</scan:UUID>
                    <scan:AdminURI>https://NPIF35BCE.local.</scan:AdminURI>
                    <scan:IconURI>https://NPIF35BCE.local./ipp/images/printer.png</scan:IconURI>
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

                /* HP Color Laser MFP 179fnw
                 <?xml version="1.0" encoding="UTF-8"?>
                        <scan:ScannerCapabilities xmlns:pwg="http://www.pwg.org/schemas/2010/12/sm"
                                              xmlns:scan="http://schemas.hp.com/imaging/escl/2011/05/03">
                        <pwg:Version>2.63</pwg:Version>
                        <pwg:MakeAndModel>HP Color Laser MFP 179fnw</pwg:MakeAndModel>
                        <pwg:SerialNumber>CNB1S3L16S</pwg:SerialNumber>
                        <scan:UUID>16a65700-007c-1000-bb49-7c4d8f87989f</scan:UUID>
                        <scan:AdminURI>http://HP7C4D8F87989F.local./sws/index.html?link=/sws/app/settings/network/AirPrint/AirPrint.html</scan:AdminURI>
                        <scan:IconURI>http://HP7C4D8F87989F.local./images/printer-icon128.png</scan:IconURI>
                        <scan:SettingProfiles>
                            <scan:SettingProfile>
                                <scan:ColorModes>
                                    <scan:ColorMode>BlackAndWhite1</scan:ColorMode>
                                    <scan:ColorMode>Grayscale8</scan:ColorMode>
                                    <scan:ColorMode>RGB24</scan:ColorMode>
                                </scan:ColorModes>
                                <scan:DocumentFormats>
                                    <pwg:DocumentFormat>application/pdf</pwg:DocumentFormat>
                                    <scan:DocumentFormatExt>application/pdf</scan:DocumentFormatExt>
                                    <pwg:DocumentFormat>image/jpeg</pwg:DocumentFormat>
                                    <scan:DocumentFormatExt>image/jpeg</scan:DocumentFormatExt>
                                </scan:DocumentFormats>
                                <scan:SupportedResolutions>
                                    <scan:DiscreteResolutions>
                                        <scan:DiscreteResolution>
                                            <scan:XResolution>100</scan:XResolution>
                                            <scan:YResolution>100</scan:YResolution>
                                        </scan:DiscreteResolution>
                                        <scan:DiscreteResolution>
                                            <scan:XResolution>200</scan:XResolution>
                                            <scan:YResolution>200</scan:YResolution>
                                        </scan:DiscreteResolution>
                                        <scan:DiscreteResolution>
                                            <scan:XResolution>300</scan:XResolution>
                                            <scan:YResolution>300</scan:YResolution>
                                        </scan:DiscreteResolution>
                                    </scan:DiscreteResolutions>
                                </scan:SupportedResolutions>
                                <scan:ColorSpaces>
                                    <scan:ColorSpace scan:default="true">YCC</scan:ColorSpace>
                                </scan:ColorSpaces>
                            </scan:SettingProfile>
                        </scan:SettingProfiles>
                        <scan:Platen>
                            <scan:PlatenInputCaps>
                                <scan:MinWidth>295</scan:MinWidth>
                                <scan:MinHeight>295</scan:MinHeight>
                                <scan:MaxWidth>2550</scan:MaxWidth>
                                <scan:MaxHeight>3507</scan:MaxHeight>
                                <scan:SettingProfiles>
                                    <scan:SettingProfile>
                                        <scan:ColorModes>
                                            <scan:ColorMode>BlackAndWhite1</scan:ColorMode>
                                            <scan:ColorMode>Grayscale8</scan:ColorMode>
                                            <scan:ColorMode>RGB24</scan:ColorMode>
                                        </scan:ColorModes>
                                        <scan:DocumentFormats>
                                            <pwg:DocumentFormat>application/pdf</pwg:DocumentFormat>
                                            <scan:DocumentFormatExt>application/pdf</scan:DocumentFormatExt>
                                            <pwg:DocumentFormat>image/jpeg</pwg:DocumentFormat>
                                            <scan:DocumentFormatExt>image/jpeg</scan:DocumentFormatExt>
                                        </scan:DocumentFormats>
                                        <scan:SupportedResolutions>
                                            <scan:DiscreteResolutions>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>100</scan:XResolution>
                                                    <scan:YResolution>100</scan:YResolution>
                                                </scan:DiscreteResolution>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>200</scan:XResolution>
                                                    <scan:YResolution>200</scan:YResolution>
                                                </scan:DiscreteResolution>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>300</scan:XResolution>
                                                    <scan:YResolution>300</scan:YResolution>
                                                </scan:DiscreteResolution>
                                            </scan:DiscreteResolutions>
                                        </scan:SupportedResolutions>
                                        <scan:ColorSpaces>
                                            <scan:ColorSpace scan:default="true">YCC</scan:ColorSpace>
                                        </scan:ColorSpaces>
                                    </scan:SettingProfile>
                                </scan:SettingProfiles>
                                <scan:SupportedIntents>
                                    <scan:Intent>Document</scan:Intent>
                                    <scan:Intent>TextAndGraphic</scan:Intent>
                                    <scan:Intent>Photo</scan:Intent>
                                    <scan:Intent>Preview</scan:Intent>
                                </scan:SupportedIntents>
                                <scan:MaxOpticalXResolution>600</scan:MaxOpticalXResolution>
                                <scan:MaxOpticalYResolution>600</scan:MaxOpticalYResolution>
                            </scan:PlatenInputCaps>
                        </scan:Platen>
                        <scan:Adf>
                            <scan:AdfSimplexInputCaps>
                                <scan:MinWidth>295</scan:MinWidth>
                                <scan:MinHeight>295</scan:MinHeight>
                                <scan:MaxWidth>2550</scan:MaxWidth>
                                <scan:MaxHeight>4200</scan:MaxHeight>
                                <scan:SettingProfiles>
                                    <scan:SettingProfile>
                                        <scan:ColorModes>
                                            <scan:ColorMode>BlackAndWhite1</scan:ColorMode>
                                            <scan:ColorMode>Grayscale8</scan:ColorMode>
                                            <scan:ColorMode>RGB24</scan:ColorMode>
                                        </scan:ColorModes>
                                        <scan:DocumentFormats>
                                            <pwg:DocumentFormat>application/pdf</pwg:DocumentFormat>
                                            <scan:DocumentFormatExt>application/pdf</scan:DocumentFormatExt>
                                            <pwg:DocumentFormat>image/jpeg</pwg:DocumentFormat>
                                            <scan:DocumentFormatExt>image/jpeg</scan:DocumentFormatExt>
                                        </scan:DocumentFormats>
                                        <scan:SupportedResolutions>
                                            <scan:DiscreteResolutions>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>100</scan:XResolution>
                                                    <scan:YResolution>100</scan:YResolution>
                                                </scan:DiscreteResolution>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>200</scan:XResolution>
                                                    <scan:YResolution>200</scan:YResolution>
                                                </scan:DiscreteResolution>
                                                <scan:DiscreteResolution>
                                                    <scan:XResolution>300</scan:XResolution>
                                                    <scan:YResolution>300</scan:YResolution>
                                                </scan:DiscreteResolution>
                                            </scan:DiscreteResolutions>
                                        </scan:SupportedResolutions>
                                        <scan:ColorSpaces>
                                            <scan:ColorSpace scan:default="true">YCC</scan:ColorSpace>
                                        </scan:ColorSpaces>
                                    </scan:SettingProfile>
                                </scan:SettingProfiles>
                                <scan:SupportedIntents>
                                    <scan:Intent>Document</scan:Intent>
                                    <scan:Intent>TextAndGraphic</scan:Intent>
                                    <scan:Intent>Photo</scan:Intent>
                                    <scan:Intent>Preview</scan:Intent>
                                </scan:SupportedIntents>
                                <scan:MaxOpticalXResolution>600</scan:MaxOpticalXResolution>
                                <scan:MaxOpticalYResolution>600</scan:MaxOpticalYResolution>
                            </scan:AdfSimplexInputCaps>
                            <scan:FeederCapacity>40</scan:FeederCapacity>
                            <scan:AdfOptions>
                                <scan:AdfOption>DetectPaperLoaded</scan:AdfOption>
                            </scan:AdfOptions>
                        </scan:Adf>
                        <scan:BlankPageDetection>false</scan:BlankPageDetection>
                        <scan:BlankPageDetectionAndRemoval>false</scan:BlankPageDetectionAndRemoval>
                    </scan:ScannerCapabilities>

                */

                #endregion

                var xmlCapabilities = SendXMLGETRequest(xmlCapabilitiesUrl, timeoutSeconds);

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

                if (dpi != -1)
                {
                    maxXScanRes = dpi.ToString();
                    maxYScanRes = dpi.ToString();
                }

                var fileExt = Path.GetExtension(fileName).ToLower();
                var documentFormatExt = fileExt == ".pdf" ? "application/pdf" : "image/jpeg";

                // sending scan request    

                #region XML exmaple

                /* 
                var scanRequest = $@"<?xml version='1.0' encoding='UTF-8'?>
                            <scan:ScanSettings xmlns:pwg=""https://www.pwg.org/schemas/2010/12/sm"" xmlns:scan=""https://schemas.hp.com/imaging/escl/2011/05/03"">
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
                              <scan:Format>Jpeg</scan:Format>
                              <scan:ContentType>Photo</scan:ContentType>
                              <scan:XResolution>{maxXScanRes}</scan:XResolution>
                              <scan:YResolution>{maxYScanRes}</scan:YResolution>
                            </scan:ScanSettings>
                            ";
                */

                #endregion

                var scanRequest = $@"<?xml version='1.0' encoding='utf-8'?>
            			<escl:ScanSettings xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:pwg=""http://www.pwg.org/schemas/2010/12/sm"" xmlns:escl=""http://schemas.hp.com/imaging/escl/2011/05/03"">
                            <pwg:Version>2.63</pwg:Version>
            				<pwg:ScanRegions pwg:MustHonor=""false"">
            					<pwg:ScanRegion>
            						<pwg:ContentRegionUnits>escl:ThreeHundredthsOfInches</pwg:ContentRegionUnits>
            						<pwg:XOffset>0</pwg:XOffset>
            						<pwg:YOffset>0</pwg:YOffset>
                                    <pwg:Width>{maxWidth}</pwg:Width>
                                    <pwg:Height>{maxHeight}</pwg:Height>
            					</pwg:ScanRegion>
            				</pwg:ScanRegions>
            				<escl:DocumentFormatExt>{documentFormatExt}</escl:DocumentFormatExt>
            				<pwg:InputSource>Platen</pwg:InputSource>
            				<escl:XResolution>{maxXScanRes}</escl:XResolution>
            				<escl:YResolution>{maxYScanRes}</escl:YResolution>
            				<escl:ColorMode>RGB24</escl:ColorMode>
            			</escl:ScanSettings>";

                var scanRequestUrl = $"{printerUrl}:8080/eSCL/ScanJobs";

                string responseLocation = null;

                using (var response = SendXMLPOSTRequest(scanRequestUrl, scanRequest, timeoutSeconds))
                {
                    foreach (var headerKey in response.Headers.AllKeys)
                    {
                        if (headerKey == "Location")
                        {
                            responseLocation = response.Headers[headerKey];
                        }
                    }
                }

                // parsing jobUri from Location:  https://10.0.0.14/eSCL/ScanJobs/46 -> /eSCL/ScanJobs/46
                var jobUri = responseLocation.Substring(responseLocation.IndexOf("/eSCL"));

                var scannedDocumentUrl = $"{printerUrl}{jobUri}/NextDocument";

                Console.WriteLine($"Scanned data url: {scannedDocumentUrl}");

                // scanning ...

                Console.WriteLine("Waiting 5 secs ...");
                Thread.Sleep(5000);


                // saving scanned data  ...
                using (var response = SendRequest(scannedDocumentUrl, "GET", timeoutSeconds))
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
