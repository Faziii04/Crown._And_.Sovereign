using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Services;
using Crown___Sovereign.Views.Dialogs.Administracion;
namespace Crown___Sovereign.Views.UserControls.Administracion.Empleados;
public partial class EmpleadosControl:UserControl{private readonly AdministracionService _service=new();public EmpleadosControl(){InitializeComponent();Refresh();}private void Refresh()=>EmployeesGrid.ItemsSource=_service.Empleados(SearchBox?.Text??"");private void Search_Changed(object s,TextChangedEventArgs e)=>Refresh();private void Create_Click(object s,RoutedEventArgs e){var d=new EmpleadoDialog{Owner=Window.GetWindow(this)};if(d.ShowDialog()==true)Refresh();}}
