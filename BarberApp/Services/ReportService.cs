using BarberApp.Models;
using System.Text;

namespace BarberApp.Services;

public class ReportService
{
    public string GenerarCsv(List<Cobro> cobros)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Hora,Servicio,Barbero,Tipo,Monto,Comision");
        foreach (var c in cobros)
            sb.AppendLine($"{c.Timestamp:yyyy-MM-dd},{c.Timestamp:HH:mm},{Esc(c.NombreServicio)},{Esc(c.NombreBarbero)},{c.Tipo},{c.Monto},{c.ComisionMonto}");
        return sb.ToString();
    }

    public string GenerarExcelXml(List<Cobro> cobros, string titulo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine("<Worksheet ss:Name=\"Reporte\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine("<Table>");
        sb.AppendLine($"<Row><Cell><Data ss:Type=\"String\">{Esc(titulo)}</Data></Cell></Row>");
        sb.AppendLine("<Row><Cell><Data ss:Type=\"String\">Fecha</Data></Cell><Cell><Data ss:Type=\"String\">Servicio</Data></Cell><Cell><Data ss:Type=\"String\">Barbero</Data></Cell><Cell><Data ss:Type=\"String\">Tipo</Data></Cell><Cell><Data ss:Type=\"String\">Monto</Data></Cell></Row>");
        foreach (var c in cobros)
        {
            sb.AppendLine("<Row>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{c.Timestamp:yyyy-MM-dd HH:mm}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{Esc(c.NombreServicio)}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{Esc(c.NombreBarbero)}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{c.Tipo}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{c.Monto}</Data></Cell>");
            sb.AppendLine("</Row>");
        }
        sb.AppendLine("</Table></Worksheet></Workbook>");
        return sb.ToString();
    }

    public byte[] GenerarPdfSimple(string titulo, IEnumerable<string> lineas)
    {
        var content = new StringBuilder();
        content.AppendLine("BT /F1 14 Tf 50 750 Td");
        content.AppendLine($"({EscPdf(titulo)}) Tj");
        content.AppendLine("0 -20 Td /F1 10 Tf");
        var y = 730;
        foreach (var linea in lineas)
        {
            y -= 14;
            if (y < 50) break;
            content.AppendLine($"0 -14 Td ({EscPdf(linea)}) Tj");
        }
        content.AppendLine("ET");

        var stream = content.ToString();
        var len = Encoding.ASCII.GetByteCount(stream);

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");
        var offsets = new List<int>();
        void Obj(int n, string body)
        {
            offsets.Add(pdf.Length);
            pdf.AppendLine($"{n} 0 obj");
            pdf.AppendLine(body);
            pdf.AppendLine("endobj");
        }

        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        Obj(4, $"<< /Length {len} >>\nstream\n{stream}\nendstream");
        Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xrefPos = pdf.Length;
        pdf.AppendLine("xref");
        pdf.AppendLine("0 6");
        pdf.AppendLine("0000000000 65535 f ");
        foreach (var off in offsets)
            pdf.AppendLine($"{off:D10} 00000 n ");
        pdf.AppendLine("trailer << /Size 6 /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefPos.ToString());
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    public string GenerarHtmlReporte(
        string titulo,
        decimal total,
        Dictionary<string, decimal> porBarbero,
        Dictionary<string, decimal> porServicio,
        (decimal Esta, decimal Anterior) comparativa)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body{font-family:sans-serif;background:#0d0d10;color:#f5f3f0;padding:20px}");
        sb.AppendLine("h1{color:#C4A77D}table{border-collapse:collapse;width:100%;margin:12px 0}");
        sb.AppendLine("td,th{border:1px solid #333;padding:8px;text-align:left}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>{EscHtml(titulo)}</h1>");
        sb.AppendLine($"<p>Total: <strong>${total:N0}</strong></p>");
        var diff = comparativa.Esta - comparativa.Anterior;
        var pct = comparativa.Anterior > 0 ? diff / comparativa.Anterior * 100 : 0;
        sb.AppendLine($"<p>Semana actual: ${comparativa.Esta:N0} vs anterior: ${comparativa.Anterior:N0} ({pct:+0;-0}%)</p>");

        sb.AppendLine("<h2>Por barbero/estilista</h2><table><tr><th>Nombre</th><th>Total</th></tr>");
        foreach (var (k, v) in porBarbero.OrderByDescending(x => x.Value))
            sb.AppendLine($"<tr><td>{EscHtml(k)}</td><td>${v:N0}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Por servicio</h2><table><tr><th>Servicio</th><th>Total</th></tr>");
        foreach (var (k, v) in porServicio.OrderByDescending(x => x.Value))
            sb.AppendLine($"<tr><td>{EscHtml(k)}</td><td>${v:N0}</td></tr>");
        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    private static string Esc(string s) => s.Contains(',') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    private static string EscPdf(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string EscHtml(string s) => System.Net.WebUtility.HtmlEncode(s);
}
