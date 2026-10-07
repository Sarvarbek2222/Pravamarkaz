using ClosedXML.Excel;
using PravaMarkaz.Models;

namespace PravaMarkaz.Services;

/// <summary>Ro'yxatlarni Excel (.xlsx) faylga chiqarish.</summary>
public static class ExcelExport
{
    private const string MoneyFormat = "# ##0";

    public static byte[] Students(IEnumerable<StudentRow> rows, string title)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("O'quvchilar");

        var headers = new[] { "№", "Familiya", "Ism", "Otasining ismi", "Markaz", "Tug'ilgan sana", "Telefon", "Toifa",
            "Holati", "Qabul sanasi", "Kurs narxi", "To'langan", "Qarz", "Oxirgi to'lov" };
        WriteTitle(ws, title, headers.Length);
        WriteHeader(ws, 3, headers);

        var r = 4;
        var i = 0;
        foreach (var s in rows)
        {
            ws.Cell(r, 1).Value = ++i;
            ws.Cell(r, 2).Value = s.LastName;
            ws.Cell(r, 3).Value = s.FirstName;
            ws.Cell(r, 4).Value = s.MiddleName ?? "";
            ws.Cell(r, 5).Value = s.CenterName;
            SetDate(ws.Cell(r, 6), s.BirthDate);
            ws.Cell(r, 7).Value = s.Phone ?? "";
            ws.Cell(r, 8).Value = s.Category ?? "";
            ws.Cell(r, 9).Value = Fmt.Label(s.Status);
            SetDate(ws.Cell(r, 10), s.EnrolledAt);
            ws.Cell(r, 11).Value = s.TotalFee;
            ws.Cell(r, 12).Value = s.Paid;
            ws.Cell(r, 13).Value = s.TotalFee - s.Paid;
            SetDate(ws.Cell(r, 14), s.LastPaidAt);
            r++;
        }

        WriteTotals(ws, r, 10, new[] { 11, 12, 13 }, 4);
        ws.Range(4, 11, r, 13).Style.NumberFormat.Format = MoneyFormat;
        if (r > 4) ws.Range(4, 13, r - 1, 13).AddConditionalFormat().WhenGreaterThan(0).Font.SetFontColor(XLColor.FromHtml("#c0392b"));
        return Finish(wb, ws, 3, headers.Length);
    }

    public static byte[] Payments(IEnumerable<PaymentRow> rows, string title)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("To'lovlar");

        var headers = new[] { "Kvitansiya №", "Sana", "O'quvchi", "Markaz", "Summa", "To'lov usuli", "Qabul qildi", "Izoh" };
        WriteTitle(ws, title, headers.Length);
        WriteHeader(ws, 3, headers);

        var r = 4;
        foreach (var p in rows)
        {
            ws.Cell(r, 1).Value = p.Id.ToString("D6");
            SetDate(ws.Cell(r, 2), p.PaidAt);
            ws.Cell(r, 3).Value = p.StudentName;
            ws.Cell(r, 4).Value = p.CenterName;
            ws.Cell(r, 5).Value = p.Amount;
            ws.Cell(r, 6).Value = Fmt.Label(p.Method);
            ws.Cell(r, 7).Value = p.CreatedBy ?? "";
            ws.Cell(r, 8).Value = p.Note ?? "";
            r++;
        }

        WriteTotals(ws, r, 4, new[] { 5 }, 4);
        ws.Range(4, 5, r, 5).Style.NumberFormat.Format = MoneyFormat;
        return Finish(wb, ws, 3, headers.Length);
    }

    private static void WriteTitle(IXLWorksheet ws, string title, int cols)
    {
        var cell = ws.Cell(1, 1);
        cell.Value = title;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, cols).Merge();
        ws.Cell(2, 1).Value = $"Yaratilgan: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private static void WriteHeader(IXLWorksheet ws, int row, string[] headers)
    {
        for (var c = 0; c < headers.Length; c++) ws.Cell(row, c + 1).Value = headers[c];
        var range = ws.Range(row, 1, row, headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a8a");
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(row).Height = 22;
    }

    private static void WriteTotals(IXLWorksheet ws, int row, int labelCol, int[] sumCols, int firstDataRow)
    {
        ws.Cell(row, labelCol).Value = "Jami:";
        foreach (var c in sumCols)
        {
            var letter = XLHelper.GetColumnLetterFromNumber(c);
            ws.Cell(row, c).FormulaA1 = row > firstDataRow ? $"SUM({letter}{firstDataRow}:{letter}{row - 1})" : "0";
        }
        var range = ws.Range(row, 1, row, sumCols.Max());
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#eef2ff");
    }

    private static void SetDate(IXLCell cell, DateTime? value)
    {
        if (value == null) return;
        cell.Value = value.Value;
        cell.Style.DateFormat.Format = "dd.mm.yyyy";
    }

    private static byte[] Finish(XLWorkbook wb, IXLWorksheet ws, int headerRow, int cols)
    {
        var last = ws.LastRowUsed()!.RowNumber();
        var table = ws.Range(headerRow, 1, last, cols);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorderColor = XLColor.FromHtml("#d0d7e2");
        table.Style.Border.OutsideBorderColor = XLColor.FromHtml("#d0d7e2");
        ws.SheetView.FreezeRows(headerRow);
        ws.Range(headerRow, 1, last - 1, cols).SetAutoFilter();
        ws.Columns(1, cols).AdjustToContents(headerRow, last);
        foreach (var col in ws.Columns(1, cols)) if (col.Width > 45) col.Width = 45;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
