using Crown___Sovereign.Data;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class IncidenciaDialog : Window{public IncidenciaDialog(){InitializeComponent();RoomBox.ItemsSource=SampleData.Habitaciones;RoomBox.SelectedIndex=0;PriorityBox.ItemsSource=new[]{"Baja","Media","Alta"};PriorityBox.SelectedItem="Media";}private void Save_Click(object sender,RoutedEventArgs e){if(RoomBox.SelectedItem is not HabitacionModel room)return;try{new MantenimientoService().Reportar(room.Id,DescriptionBox.Text,PriorityBox.Text,BlockBox.IsChecked==true);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar",MessageBoxButton.OK,MessageBoxImage.Warning);}}private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();}
