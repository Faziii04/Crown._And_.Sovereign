using Crown___Sovereign.Data;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class ObjetoOlvidadoDialog : Window{public ObjetoOlvidadoDialog(){InitializeComponent();RoomBox.ItemsSource=SampleData.Habitaciones;RoomBox.SelectedIndex=0;}private void Save_Click(object sender,RoutedEventArgs e){try{new ObjetosOlvidadosService().Registrar(DescriptionBox.Text,(RoomBox.SelectedItem as HabitacionModel)?.Id,NotesBox.Text);DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"No se pudo guardar",MessageBoxButton.OK,MessageBoxImage.Warning);}}private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();}
