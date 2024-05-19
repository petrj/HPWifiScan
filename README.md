# HPWifiScan

Command line scanning tool for HP network printers

- Mono & .NET
- Linux & Windows compatibile
- Scanning with detected maximum width, height and resolution
- Scanning in jpeg or pdf format (detected by extension)

- Usage:

  `HPWifiScan.exe PrinterIP [filename] [dpi]`

 default fileName is scan.jpg
 default DPI is maximum supported by printer


- Examples:

  `HPWifiScan.exe 10.0.0.3`

  `HPWifiScan.exe 192.168.1.12 MyScannedFile.jpg`
  
  `HPWifiScan.exe 192.168.1.12 document.pdf`

  `HPWifiScan.exe 192.168.1.12 document-300DPI.pdf 300`


- Building:

  `msbuild HPWifiScan.sln`


Thanks to:

- https://github.com/kno10/python-scan-eSCL
- https://mamascode.wordpress.com/2015/04/07/scanning-from-wifi-hp-scanner/
