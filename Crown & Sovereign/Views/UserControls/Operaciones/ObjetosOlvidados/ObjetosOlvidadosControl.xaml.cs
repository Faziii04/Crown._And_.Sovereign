using Crown___Sovereign.Data;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Operaciones;
namespace Crown___Sovereign.Views.UserControls.Operaciones.ObjetosOlvidados;
public partial class ObjetosOlvidadosControl : UserControl
{private readonly ObjetosOlvidadosService _service=new();public ObjetosOlvidadosControl(){InitializeComponent();Refresh();}private void Refresh()=>ItemsGrid.ItemsSource=_service.Listar(SearchBox?.Text??"").Select(o=>new{Objeto=o, o.Descripcion,Habitacion=o.HabitacionId is int id?SampleData.Habitaciones.FirstOrDefault(r=>r.Id==id)?.Numero??"—":"—",o.Encontrado,o.Estado,o.Observaciones}).ToList();private void Search_Changed(object sender,TextChangedEventArgs e)=>Refresh();private void Create_Click(object sender,RoutedEventArgs e){var d=new ObjetoOlvidadoDialog{Owner=Window.GetWindow(this)};if(d.ShowDialog()==true)Refresh();}private void Return_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not object row)return;var item=(ObjetoOlvidadoModel)row.GetType().GetProperty("Objeto")!.GetValue(row)!;_service.Devolver(item.Id);Refresh();}}
