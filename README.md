# 🖨️ Office Print Server

by **Bagus Pradika** — 2026

Internal print system untuk LAN Windows. Gantikan Windows Printer Sharing (Point & Print) yang bermasalah dengan **Standard TCP/IP Port (port 9100)**.

## Fitur

| Fitur | Keterangan |
|-------|-----------|
| **Raw TCP port 9100** | Koneksi stabil, kompatibel semua Windows |
| **IPP port 18080** | Alternatif protokol standar |
| **WPF Dashboard** | Drag-drop PDF + monitoring print job |
| **SQLite** | Riwayat print job |
| **Windows Service** | Auto-start, restart otomatis jika crash |

## Cara Pakai

### Server

**Versi Full** (142 MB, no runtime needed):
```
OfficePrintSystem\Publish\Server\Install-Server.bat
```

**Versi Slim** (36 MB, butuh .NET 8 Runtime):
```
officePrintByBag\Install-Server.bat
```
.NET 8 akan otomatis didownload jika belum terinstall.

### Client (3 Metode)

| Metode | Cara |
|--------|------|
| **A. Standard TCP/IP Port ✅** | Add Printer > IP: `193.13.7.17` Port: `9100` |
| **B. IPP** | URL: `http://193.13.7.17:18080/ipp/EPSON L1110 Series` |
| **C. WPF Dashboard** | `OfficePrintClient.WPF.exe` — drag-drop PDF |

## Struktur Repo

```
📂 Print Server
├── OfficePrintSystem\          ← Source code + versi full
│   ├── Publish\Server\         ← Server full installer
│   ├── Publish\Client\         ← Client WPF + installer
│   ├── Installers\             ← PS1 + Inno Setup
│   ├── OfficePrintServer.API\  ← API controllers
│   ├── OfficePrintServer.Infrastructure\ ← Services (Printer, IPP, TCP)
│   └── OfficePrintServer.Service\ ← Windows Service host
├── officePrintByBag\           ← Versi slim (36 MB)
├── Tutorial Office Print Server.docx ← Panduan lengkap
└── .gitignore
```

## Port

| Port | Protokol | Fungsi |
|------|----------|--------|
| 9100 | Raw TCP | Print via Standard TCP/IP Port |
| 18080 | HTTP/IPP | Print via IPP + REST API |

## Tech Stack

.NET 8, ASP.NET Core, EF Core + SQLite, Serilog, Windows.Data.Pdf, winspool.drv P/Invoke, WPF
