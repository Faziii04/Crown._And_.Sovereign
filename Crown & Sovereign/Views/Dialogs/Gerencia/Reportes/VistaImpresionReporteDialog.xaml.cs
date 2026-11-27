using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.Dialogs.Gerencia.Reportes;

public partial class VistaImpresionReporteDialog : Window
{
    public VistaImpresionReporteDialog(ResultadoReporte result)
    {
        InitializeComponent();
        Title = $"Imprimir · {result.Titulo}";
        DocumentViewer.Document = BuildDocument(result);
    }
    private static FlowDocument BuildDocument(ResultadoReporte r)
    {
        var document = new FlowDocument { PagePadding = new Thickness(48), ColumnWidth = double.PositiveInfinity, FontFamily = new FontFamily("Segoe UI"), FontSize = 10 };
        document.Blocks.Add(new Paragraph(new Run("CROWN & SOVEREIGN · HOTEL PMS")) { FontSize = 10, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 0, 0, 8) });
        document.Blocks.Add(new Paragraph(new Run(r.Titulo)) { FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        document.Blocks.Add(new Paragraph(new Run($"{r.Categoria} · Periodo {r.Desde:dd/MM/yyyy} – {r.Hasta:dd/MM/yyyy}\nGenerado {r.Generado:dd/MM/yyyy HH:mm} por {r.Usuario}\n{r.Parametros}")) { Margin = new Thickness(0, 0, 0, 16) });
        document.Blocks.Add(new Paragraph(new Run("Indicadores")) { FontSize = 14, FontWeight = FontWeights.SemiBold });
        var metrics = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };
        metrics.Columns.Add(new TableColumn { Width = new GridLength(210) }); metrics.Columns.Add(new TableColumn { Width = new GridLength(220) }); metrics.Columns.Add(new TableColumn { Width = new GridLength(290) });
        var mg = new TableRowGroup(); metrics.RowGroups.Add(mg); mg.Rows.Add(Row("Indicador", "Valor", "Contexto", true));
        foreach (var item in r.Indicadores) mg.Rows.Add(Row(item.Nombre, item.Valor, item.Contexto)); document.Blocks.Add(metrics);
        if (r.Detalle.Rows.Count > 0)
        {
            document.Blocks.Add(new Paragraph(new Run("Detalle de datos")) { FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 6) });
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };
            foreach (System.Data.DataColumn _ in r.Detalle.Columns) table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var group = new TableRowGroup(); table.RowGroups.Add(group);
            var header = new TableRow();
            foreach (System.Data.DataColumn column in r.Detalle.Columns) header.Cells.Add(Cell(column.ColumnName, true));
            group.Rows.Add(header);
            foreach (System.Data.DataRow row in r.Detalle.Rows)
            {
                var dataRow = new TableRow();
                foreach (var value in row.ItemArray) dataRow.Cells.Add(Cell(Format(value)));
                group.Rows.Add(dataRow);
            }
            document.Blocks.Add(table);
        }
        document.Blocks.Add(new Paragraph(new Run("Metodología y limitaciones")) { FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 6) });
        document.Blocks.Add(new Paragraph(new Run(r.Metodologia)) { TextAlignment = TextAlignment.Left });
        foreach (var warning in r.Advertencias) document.Blocks.Add(new Paragraph(new Run("• " + warning)) { Foreground = Brushes.DarkGoldenrod, Margin = new Thickness(0, 3, 0, 0) });
        return document;
    }
    private static TableRow Row(string a, string b, string c, bool header = false) => new() { Cells = { Cell(a, header), Cell(b, header), Cell(c, header) } };
    private static TableCell Cell(string text, bool header = false) => new(new Paragraph(new Run(text)) { Margin = new Thickness(3) }) { Padding = new Thickness(3), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(.25), FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal, Background = header ? new SolidColorBrush(Color.FromRgb(242, 245, 248)) : Brushes.White };
    private static string Format(object? x) => x switch { null or DBNull => "—", DateTime d => d.ToString("dd/MM/yyyy HH:mm"), decimal m => m.ToString("N2"), _ => Convert.ToString(x) ?? "—" };
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() == true && DocumentViewer.Document is FlowDocument document)
        {
            document.PageWidth = dialog.PrintableAreaWidth; document.PageHeight = dialog.PrintableAreaHeight;
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, Title);
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
