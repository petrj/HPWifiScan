# HPWifiScan

Command line scanning tool for HP network printers

- Mono & .NET
- Linux & Windows compatibile
- Scanning with detected maximum width, height and resolution
- Scanning in jpeg or pdf format (detected by extension)

- Usage:

  `HPWifiScan.exe PrinterIP [filename]`

 default fileName is scan.jpg


- Examples:

  `HPWifiScan.exe 10.0.0.3`

  `HPWifiScan.exe 192.168.1.12 MyScannedFile.jpg`
  
  `HPWifiScan.exe 192.168.1.12 document.pdf`



Thanks to:
    - https://github.com/kno10/python-scan-eSCL    
    - https://mamascode.wordpress.com/2015/04/07/scanning-from-wifi-hp-scanner/
