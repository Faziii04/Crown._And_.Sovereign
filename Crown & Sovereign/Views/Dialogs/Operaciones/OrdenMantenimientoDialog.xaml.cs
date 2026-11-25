using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class OrdenMantenimientoDialog : Window{private readonly IncidenciaModel _incident;public OrdenMantenimientoDialog(IncidenciaModel incident){InitializeComponent();_incident=incident;IncidentText.Text=incident.Descripcion;DatePicker.SelectedDate=DateTime.Today.AddDays(1);}private void Save_Click(object sender,RoutedEventArgs e){try{new MantenimientoService().CrearOrden(_incident,AssigneeBox.Text,DatePicker.SelectedDate);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo crear",MessageBoxButton.OK,MessageBoxImage.Warning);}}private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();}
