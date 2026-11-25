using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
public partial class DirectorioHuespedesControl : UserControl
{
 private readonly HuespedService _service=new(); private List<HuespedDirectorioFila> _rows=[];
 public event Action<HuespedModel?>? SeleccionCambiada; public event Action? FiltroCambiado;
 public HuespedModel? HuespedSeleccionado => GuestList.SelectedItem is HuespedDirectorioFila row?_service.Obtener(row.Id):null;
 public string EstadoRapido {get;set;}="Todos";
 public DirectorioHuespedesControl(){InitializeComponent();Filters.FiltroCambiado+=ApplyFilter;}
 public void BuscarHuesped(int id){Filters.FiltroCambiado-=ApplyFilter;Filters.Limpiar();Filters.FiltroCambiado+=ApplyFilter;EstadoRapido="Todos";Refrescar(id);}
 public void Refrescar(int? selectedId=null){var filter=Filters.ObtenerFiltro();filter.Estado=EstadoRapido;_rows=_service.BuscarDirectorio(filter).ToList();GuestList.ItemsSource=_rows;CountText.Text=$"{_rows.Count} huésped{(_rows.Count==1?"":"es")} encontrado{(_rows.Count==1?"":"s")}";var selected=_rows.FirstOrDefault(item=>item.Id==selectedId);GuestList.SelectedItem=selected??_rows.FirstOrDefault();UpdateEmpty();}
 public void AplicarFiltroResumen(string filter){Filters.FiltroCambiado-=ApplyFilter;Filters.Limpiar();Filters.FiltroCambiado+=ApplyFilter;EstadoRapido=filter;Refrescar();}
 private void UpdateEmpty(){var empty=_rows.Count==0;EmptyState.Visibility=empty?Visibility.Visible:Visibility.Collapsed;GuestList.Visibility=empty?Visibility.Collapsed:Visibility.Visible;}
 private void ApplyFilter(){EstadoRapido=Filters.ObtenerFiltro().Estado;Refrescar(GuestList.SelectedItem is HuespedDirectorioFila row?row.Id:null);FiltroCambiado?.Invoke();}
 private void Guest_SelectionChanged(object s,SelectionChangedEventArgs e)=>SeleccionCambiada?.Invoke(HuespedSeleccionado);
 private void Clear_Click(object s,RoutedEventArgs e){Filters.Limpiar();EstadoRapido="Todos";Refrescar();FiltroCambiado?.Invoke();}
}
