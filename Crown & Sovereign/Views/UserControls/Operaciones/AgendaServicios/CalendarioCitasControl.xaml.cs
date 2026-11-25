using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;

namespace Crown___Sovereign.Views.UserControls.Operaciones.AgendaServicios;

public partial class CalendarioCitasControl : UserControl
{
    private readonly CitaServicioService _service = new();
    public event Action? AgendaActualizada;
    public CalendarioCitasControl()
    {
        InitializeComponent();
        DateFilter.SelectedDate = DateTime.Today;
        ServiceFilter.ItemsSource = new[] { "Todas las áreas" }.Concat(SampleData.Servicios.Select(item => item.Categoria).Distinct().Order()).ToList(); ServiceFilter.SelectedIndex = 0;
        EmployeeFilter.ItemsSource = new[] { "Todo el personal" }.Concat(SampleData.Citas.Select(item => item.AsignadoA).Distinct().Order()).ToList(); EmployeeFilter.SelectedIndex = EmployeeFilter.Items.Count > 1 ? 1 : 0;
        Recargar();
    }
    public void Recargar()
    {
        ScheduleGrid.Children.Clear(); ScheduleGrid.RowDefinitions.Clear(); ScheduleGrid.ColumnDefinitions.Clear();
        var date = DateFilter.SelectedDate ?? DateTime.Today;
        var category = ServiceFilter.SelectedItem?.ToString(); var employee = EmployeeFilter.SelectedItem?.ToString();
        var rows = _service.ConsultarAgenda(date, category, employee);
        var employees = (employee is null or "Todo el personal" ? rows.Select(item => item.Employee).Distinct().ToList() : new List<string> { employee });
        if (employees.Count == 0) employees.Add("Sin citas");
        ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        foreach (var _ in employees) ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var hour = 8; hour < 20; hour++) ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
        AddCell("Hora", 0, 0, true);
        for (var column = 0; column < employees.Count; column++) AddCell(employees[column], 0, column + 1, true);
        for (var hour = 8; hour < 20; hour++)
        {
            AddCell($"{hour:00}:00", hour - 7, 0, false);
            for (var column = 0; column < employees.Count; column++)
            {
                var gridCell = new Border { BorderBrush = (Brush)FindResource("Line"), BorderThickness = new Thickness(0, 0, 1, 1), Background = Brushes.White };
                Grid.SetRow(gridCell, hour - 7); Grid.SetColumn(gridCell, column + 1); ScheduleGrid.Children.Add(gridCell);
            }
        }
        foreach (var appointment in rows)
        {
            var column = employees.IndexOf(appointment.Employee);
            if (column < 0) continue;
            var row = appointment.Start.Hour - 7;
            if (row is < 1 or > 12) continue;
            var block = new Border { Background = appointment.Status == "En curso" ? new SolidColorBrush(Color.FromRgb(220, 237, 229)) : appointment.Status == "Solicitada" ? new SolidColorBrush(Color.FromRgb(246, 236, 217)) : new SolidColorBrush(Color.FromRgb(223, 234, 243)), BorderBrush = new SolidColorBrush(Color.FromRgb(201, 215, 228)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6), Margin = new Thickness(3), ToolTip = $"{appointment.Guest} · Hab. {appointment.Room} · {appointment.Status}", Tag = appointment.Id };
            block.Child = new StackPanel { Children = { new TextBlock { Text = appointment.Service, FontWeight = FontWeights.SemiBold, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis }, new TextBlock { Text = $"{appointment.Guest} · Hab. {appointment.Room}", FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis }, new TextBlock { Text = appointment.Status, FontSize = 9 } } };
            block.MouseLeftButtonDown += Appointment_DoubleClick;
            Grid.SetRow(block, row); Grid.SetColumn(block, column + 1); Grid.SetRowSpan(block, Math.Max(1, (int)Math.Ceiling((appointment.End - appointment.Start).TotalHours))); Panel.SetZIndex(block, 1); ScheduleGrid.Children.Add(block);
        }
    }
    private void AddCell(string text, int row, int column, bool header)
    {
        var border = new Border { Background = header ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) : Brushes.White, BorderBrush = (Brush)FindResource("Line"), BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(8) };
        border.Child = new TextBlock { Text = text, FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal, Foreground = header ? (Brush)FindResource("Muted") : (Brush)FindResource("Ink"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(border, row); Grid.SetColumn(border, column); ScheduleGrid.Children.Add(border);
    }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Recargar(); }
    private void Previous_Click(object sender, RoutedEventArgs e) { DateFilter.SelectedDate = (DateFilter.SelectedDate ?? DateTime.Today).AddDays(-1); }
    private void Today_Click(object sender, RoutedEventArgs e) { DateFilter.SelectedDate = DateTime.Today; }
    private void Next_Click(object sender, RoutedEventArgs e) { DateFilter.SelectedDate = (DateFilter.SelectedDate ?? DateTime.Today).AddDays(1); }
    private void Appointment_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not Border { Tag: int id }) return;
        var appointment = _service.Listar().FirstOrDefault(item => item.Id == id); if (appointment is null) return;
        if (new GestionarCitaDialog(appointment) { Owner = Window.GetWindow(this) }.ShowDialog() == true) { Recargar(); AgendaActualizada?.Invoke(); }
    }
}
