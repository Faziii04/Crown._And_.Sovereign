using Crown___Sovereign.Data;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Limpieza;
public partial class TableroLimpiezaControl : UserControl
{
 private readonly LimpiezaService _service=new(); public TableroLimpiezaControl(){InitializeComponent();StateBox.ItemsSource=new[]{"Todos los estados","Pendiente","En progreso","Completada","Inspeccionada"};StateBox.SelectedIndex=0;Refresh();}
 private void Refresh()=>TaskGrid.ItemsSource=_service.Listar(StateBox.SelectedItem as string).Select(t=>new{Tarea=t,Habitacion=SampleData.Habitaciones.First(r=>r.Id==t.Habitacion).Numero,t.Piso,t.Tipo,t.Empleado,t.Hora,t.Prioridad,t.Estado}).ToList();
 private void Filter_Changed(object sender,SelectionChangedEventArgs e){if(TaskGrid is not null)Refresh();}
 private void Create_Click(object sender,RoutedEventArgs e){var d=new TareaLimpiezaDialog{Owner=Window.GetWindow(this)};if(d.ShowDialog()==true)Refresh();}
 private void Advance_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not object row)return;var task=(TareaModel)row.GetType().GetProperty("Tarea")!.GetValue(row)!;try{if(task.Estado=="Pendiente")_service.Iniciar(task);else if(task.Estado=="En progreso")_service.Completar(task);else if(task.Estado=="Completada")_service.Inspeccionar(task,MessageBox.Show(Window.GetWindow(this),"¿La habitación está lista?","Inspección",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes);Refresh();}catch(Exception ex){MessageBox.Show(Window.GetWindow(this),ex.Message,"No se pudo actualizar",MessageBoxButton.OK,MessageBoxImage.Warning);}}
}
