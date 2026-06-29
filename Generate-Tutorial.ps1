# Generate Tutorial DOCX
$path = "F:\My Lab\opencode\Print Server\Tutorial Office Print Server.docx"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Add()

function w($t) { $word.Selection.TypeText($t) }
function p() { $word.Selection.TypeParagraph() }
function bold($s) { $word.Selection.Font.Bold = $true; w($s); $word.Selection.Font.Bold = $false }

w("OFFICE PRINT SERVER"); p()
w("Panduan Koneksi Client ke Server Print"); p()
w("Created by Bagus Pradika | 2026"); p(); p()

w("Ada 3 metode untuk menghubungkan komputer client ke printer server."); p(); p()

# METHOD A
bold("METODE A - Standard TCP/IP Port (RECOMMENDED)"); p()
w("Metode paling stabil. Print job dikirim langsung ke port 9100."); p(); p()
$stepsA = @(
    "Control Panel > Devices and Printers > Add Printer",
    "Add a printer using an IP address or hostname",
    "Device type: TCP/IP Device",
    "Hostname or IP: 193.13.7.17",
    "Uncheck 'Query the printer and automatically select the driver'",
    "Klik Next (abaikan not detected, pilih Standard)",
    "Pilih driver: EPSON L1110 Series (atau L3110/L3210)",
    "Finish",
    "Printer Properties > Ports > Configure Port",
    "Protocol: Raw, Port: 9100, Uncheck SNMP Status Enabled"
)
for($i=0;$i -lt $stepsA.Length;$i++){ w("$($i+1). $($stepsA[$i])"); p() }
p()

# METHOD B
bold("METODE B - IPP (Alternatif)"); p()
w("Metode alternatif menggunakan protokol IPP. Mungkin gagal di beberapa versi Windows."); p(); p()
$stepsB = @(
    "Control Panel > Devices and Printers > Add Printer",
    "Add a printer using an IP address or hostname",
    "Device type: TCP/IP Device",
    "Hostname: 193.13.7.17",
    "Atau pilih Create a new port > Standard TCP/IP Port",
    "Atau URL: http://193.13.7.17:18080/ipp/EPSON L1110 Series",
    "Pilih driver > Finish"
)
for($i=0;$i -lt $stepsB.Length;$i++){ w("$($i+1). $($stepsB[$i])"); p() }
p()

# METHOD C
bold("METODE C - WPF Dashboard"); p()
w("Aplikasi Windows untuk drag-drop PDF dan monitoring print job."); p(); p()
$stepsC = @(
    "Jalankan OfficePrintClient.WPF.exe (dari Publish\Client\)",
    "Dashboard muncul dengan daftar printer server",
    "Drag-drop file PDF atau klik Print",
    "Status print job akan muncul di dashboard"
)
for($i=0;$i -lt $stepsC.Length;$i++){ w("$($i+1). $($stepsC[$i])"); p() }
p()

# Troubleshooting
bold("TROUBLESHOOTING"); p(); p()
$troubles = @(
    @("Test koneksi:", "Test-NetConnection 193.13.7.17 -Port 9100"),
    @("Printer tidak muncul:", "Pastikan firewall server buka port 9100"),
    @("Error device not found:", "Pilih Standard, uncheck SNMP"),
    @("Driver tidak ada:", "Gunakan Epson L3110/L3210 sebagai alternatif"),
    @("Test page gagal:", "Cek log server di C:\Program Files\OfficePrintServer\logs\"),
    @("Cek API:", "Invoke-RestMethod http://193.13.7.17:18080/api/printers")
)
foreach($t in $troubles){ bold($t[0]); p(); w($t[1]); p(); p() }

# Info
bold("INFORMASI SERVER"); p()
w("IP Server: 193.13.7.17"); p()
w("Raw TCP Port: 9100"); p()
w("IPP Port: 18080"); p()
w("Service: OfficePrintServer"); p()
w("Install di: C:\Program Files\OfficePrintServer"); p()
w("Restart: Restart-Service OfficePrintServer"); p()
w("Log: Get-Content C:\Program Files\OfficePrintServer\logs\*.log -Tail 20"); p()

$doc.SaveAs([ref]$path, [ref]16)
$doc.Close()
$word.Quit()
Write-Host "Tutorial berhasil dibuat: $path"
