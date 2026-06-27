using System.Text;

namespace OfficePrintServer.Infrastructure.Services;

public class IppParser
{
    public class IppRequest
    {
        public short Version { get; set; }
        public short OperationId { get; set; }
        public int RequestId { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string JobOriginatingUserName { get; set; } = "unknown";
        public byte[] DocumentData { get; set; } = Array.Empty<byte>();
        public bool HasDocument => DocumentData.Length > 0;
        public string DocumentFormat { get; set; } = "application/pdf";
    }

    public static IppRequest Parse(byte[] data, string? urlPrinterName = null)
    {
        var request = new IppRequest();
        int offset = 0;

        if (data.Length < 8) return request;

        request.Version = (short)((data[0] << 8) | data[1]);
        offset += 2;

        request.OperationId = (short)((data[2] << 8) | data[3]);
        offset += 2;

        request.RequestId = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
        offset += 4;

        int documentStart = -1;

        while (offset < data.Length)
        {
            if (offset >= data.Length) break;
            byte tag = data[offset];
            offset++;

            if (tag >= 0x01 && tag <= 0x06)
            {
                // Begin attributes group - continue parsing
                continue;
            }
            else if (tag == 0x7f)
            {
                // End-of-attributes - remaining data is document
                documentStart = offset;
                break;
            }
            else if (tag == 0x03)
            {
                // End-of-attributes marker
                documentStart = offset;
                break;
            }
            else
            {
                // Attribute tag
                if (offset + 2 > data.Length) break;
                int nameLen = (data[offset] << 8) | data[offset + 1];
                offset += 2;

                string name = "";
                if (nameLen > 0 && offset + nameLen <= data.Length)
                {
                    name = Encoding.UTF8.GetString(data, offset, nameLen);
                    offset += nameLen;
                }

                if (offset + 2 > data.Length) break;
                int valueLen = (data[offset] << 8) | data[offset + 1];
                offset += 2;

                string value = "";
                if (valueLen > 0 && offset + valueLen <= data.Length)
                {
                    value = Encoding.UTF8.GetString(data, offset, valueLen);
                    offset += valueLen;
                }
                else if (valueLen > 0)
                {
                    offset = data.Length;
                    break;
                }

                switch (name)
                {
                    case "job-name":
                    case "job-name-in-request":
                        request.JobName = value;
                        break;
                    case "printer-uri":
                        request.PrinterName = ExtractPrinterNameFromUri(value) ?? urlPrinterName ?? value;
                        break;
                    case "job-originating-user-name":
                        request.JobOriginatingUserName = value;
                        break;
                    case "document-format":
                    case "document-format-requested":
                        request.DocumentFormat = value;
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(request.PrinterName) && urlPrinterName != null)
            request.PrinterName = urlPrinterName;

        if (string.IsNullOrEmpty(request.JobName))
            request.JobName = "Unknown Document";

        // Extract document data (could be after end-of-attributes)
        if (documentStart > 0 && documentStart < data.Length)
        {
            request.DocumentData = new byte[data.Length - documentStart];
            Array.Copy(data, documentStart, request.DocumentData, 0, request.DocumentData.Length);
        }

        return request;
    }

    private static string? ExtractPrinterNameFromUri(string uri)
    {
        try
        {
            if (uri.Contains("/ipp/"))
            {
                var parts = uri.Split(new[] { "/ipp/" }, StringSplitOptions.None);
                if (parts.Length >= 2)
                    return Uri.UnescapeDataString(parts[1].Split('/')[0]);
            }
        }
        catch { }
        return null;
    }

    public static byte[] BuildGetPrinterAttributesResponse(string printerName)
    {
        return BuildIppResponse(0x00, printerName, responseType: "attributes");
    }

    public static byte[] BuildValidateJobResponse()
    {
        return BuildIppResponse(0x00, "default", responseType: "validate");
    }

    public static byte[] BuildPrintJobResponse()
    {
        return BuildIppResponse(0x00, "default", responseType: "job");
    }

    private static byte[] BuildIppResponse(short statusCode, string printerName, string responseType)
    {
        using var ms = new MemoryStream();
        using var bw = new BigEndianWriter(ms);

        bw.Write((short)0x0201); // IPP version 2.1 (more compatible with Windows 10/11)
        bw.Write(statusCode);    // status code
        bw.Write(1);             // request-id

        // Operation attributes group
        bw.Write((byte)0x01); // start operation-attributes

        WriteAttribute(bw, 0x47, "attributes-charset", "utf-8");
        WriteAttribute(bw, 0x48, "attributes-natural-language", "en-us");

        var printerUri = $"ipp://localhost:18080/ipp/{Uri.EscapeDataString(printerName)}";
        WriteAttribute(bw, 0x45, "printer-uri", printerUri);
        WriteAttribute(bw, 0x44, "status-code", "successful-ok");

        // Printer attributes group
        bw.Write((byte)0x04); // start printer-attributes (0x04 = printer-attributes-group tag)
        // Note: 0x04 is printer attributes, but some Windows versions expect specific tags.
        // Using 0x02 (printer-attributes in newer specs)

        WriteAttribute(bw, 0x41, "printer-name", printerName);
        WriteAttribute(bw, 0x41, "printer-location", "Office Print Server");
        WriteAttribute(bw, 0x41, "printer-info", "Office Print Server IPP Printer");
        WriteAttribute(bw, 0x41, "printer-make-and-model", "OfficePrintSystem v1.0");
        WriteAttribute(bw, 0x21, "printer-state", "3"); // idle
        WriteAttribute(bw, 0x44, "printer-state-reasons", "none");
        WriteAttribute(bw, 0x44, "printer-is-accepting-jobs", "true");
        WriteAttribute(bw, 0x21, "queued-job-count", "0");
        WriteAttribute(bw, 0x44, "pdl-override-supported", "not-attempted");
        WriteAttribute(bw, 0x44, "printer-charge-info", "none");

        // Supported document formats (critical for Windows to recognize printer)
        WriteAttribute(bw, 0x44, "document-format-supported", "application/octet-stream");
        WriteAttribute(bw, 0x44, "document-format-supported", "application/pdf");
        WriteAttribute(bw, 0x44, "document-format-supported", "image/pwg-raster");
        WriteAttribute(bw, 0x44, "document-format-supported", "image/jpeg");
        WriteAttribute(bw, 0x44, "document-format-supported", "image/tiff");
        WriteAttribute(bw, 0x44, "document-format-supported", "text/plain");
        WriteAttribute(bw, 0x44, "document-format-supported", "application/vnd.hp-pcl");
        WriteAttribute(bw, 0x44, "document-format-supported", "application/postscript");

        // Operations supported
        WriteAttribute(bw, 0x21, "operations-supported", "2");    // Print-Job
        WriteAttribute(bw, 0x21, "operations-supported", "3");    // Print-URI
        WriteAttribute(bw, 0x21, "operations-supported", "4");    // Validate-Job
        WriteAttribute(bw, 0x21, "operations-supported", "5");    // Create-Job
        WriteAttribute(bw, 0x21, "operations-supported", "9");    // Get-Printer-Attributes
        WriteAttribute(bw, 0x21, "operations-supported", "10");   // Get-Jobs
        WriteAttribute(bw, 0x21, "operations-supported", "11");   // Get-Job-Attributes
        WriteAttribute(bw, 0x21, "operations-supported", "12");   // Cancel-Job
        WriteAttribute(bw, 0x21, "operations-supported", "19");   // Cancel-My-Jobs

        // Other capabilities
        WriteAttribute(bw, 0x44, "color-supported", "true");
        WriteAttribute(bw, 0x44, "sides-supported", "one-sided");
        WriteAttribute(bw, 0x44, "sides-supported", "two-sided-long-edge");
        WriteAttribute(bw, 0x44, "orientation-requested-supported", "3"); // portrait
        WriteAttribute(bw, 0x44, "orientation-requested-supported", "4"); // landscape
        WriteAttribute(bw, 0x44, "printer-resolution-supported", "300dpi");
        WriteAttribute(bw, 0x44, "printer-resolution-supported", "600dpi");
        WriteAttribute(bw, 0x44, "media-supported", "iso_a4_210x297mm");
        WriteAttribute(bw, 0x44, "media-supported", "na_letter_8.5x11in");
        WriteAttribute(bw, 0x44, "media-supported", "na_legal_8.5x14in");
        WriteAttribute(bw, 0x44, "media-supported", "iso_a5_148x210mm");

        // IPP Everywhere / AirPrint attributes
        WriteAttribute(bw, 0x41, "ipp-versions-supported", "1.1");
        WriteAttribute(bw, 0x41, "ipp-versions-supported", "2.0");
        WriteAttribute(bw, 0x44, "multiple-document-jobs-supported", "false");
        WriteAttribute(bw, 0x44, "charset-configured", "utf-8");
        WriteAttribute(bw, 0x44, "charset-supported", "utf-8");
        WriteAttribute(bw, 0x44, "natural-language-configured", "en-us");
        WriteAttribute(bw, 0x44, "natural-language-supported", "en-us");
        WriteAttribute(bw, 0x44, "uri-authentication-supported", "none");
        WriteAttribute(bw, 0x44, "uri-security-supported", "none");
        WriteAttribute(bw, 0x44, "printer-uri-supported", printerUri);

        bw.Write((byte)0x03); // end-of-attributes

        return ms.ToArray();
    }

    private static void WriteAttribute(BigEndianWriter bw, byte tag, string name, string value)
    {
        bw.Write(tag);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        bw.Write((short)nameBytes.Length);
        if (nameBytes.Length > 0)
            bw.Write(nameBytes);
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        bw.Write((short)valueBytes.Length);
        bw.Write(valueBytes);
    }

    private class BigEndianWriter : BinaryWriter
    {
        public BigEndianWriter(Stream s) : base(s) { }

        public override void Write(short value)
        {
            base.Write((byte)((value >> 8) & 0xFF));
            base.Write((byte)(value & 0xFF));
        }

        public override void Write(int value)
        {
            base.Write((byte)((value >> 24) & 0xFF));
            base.Write((byte)((value >> 16) & 0xFF));
            base.Write((byte)((value >> 8) & 0xFF));
            base.Write((byte)(value & 0xFF));
        }
    }
}
