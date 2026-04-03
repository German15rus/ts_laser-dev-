using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class ExportService
{
    public FileContentResult BuildCsv(string[] headers, IReadOnlyCollection<string[]> rows, string filename)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', headers));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(';', row.Select(EscapeCsvField)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

        return new FileContentResult(bytes, "text/csv")
        {
            FileDownloadName = $"{filename}.csv"
        };
    }

    public FileContentResult BuildXlsx(string[] headers, IReadOnlyCollection<string[]> rows, string filename)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Data");

        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Length; col++)
            {
                sheet.Cell(rowIndex, col + 1).Value = row[col];
            }

            rowIndex++;
        }

        sheet.Columns().AdjustToContents(5, 50);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return new FileContentResult(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = $"{filename}.xlsx"
        };
    }

    public static string FormatDate(DateOnly? date)
    {
        return date?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
