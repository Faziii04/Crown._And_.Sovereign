using Crown___Sovereign.Data;
using System.Windows;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.Dialogs.Operaciones;
public partial class TareaLimpiezaDialog : Window
{private readonly LimpiezaService _service=new();public TareaLimpiezaDialog(){InitializeComponent();RoomBox.ItemsSource=SampleData.Habitaciones;RoomBox.SelectedIndex=0;TypeBox.ItemsSource=new[]{"Repaso","Salida","Inspección","Preparación"};TypeBox.SelectedIndex=0;PriorityBox.ItemsSource=new[]{"Normal","Alta","Urgente"};PriorityBox.SelectedIndex=0;}private void Save_Click(object sender,RoutedEventArgs e){if(RoomBox.SelectedItem is not HabitacionModel room)return;_service.Crear(room.Id,TypeBox.Text,EmployeeBox.Text,PriorityBox.Text,DateTime.Today.AddHours(9));DialogResult=true;}private void Cancel_Click(object sender,RoutedEventArgs e)=>Close();}
