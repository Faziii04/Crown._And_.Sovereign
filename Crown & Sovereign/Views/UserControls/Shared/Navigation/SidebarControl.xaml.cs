using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;

namespace Crown___Sovereign.Views.UserControls.Shared.Navigation;

public partial class SidebarControl : UserControl
{
    private readonly ObservableCollection<NavigationEntry> _entries = [];
    public event EventHandler<string>? NavigationRequested;
    public event EventHandler? LogoutRequested;

    public SidebarControl()
    {
        InitializeComponent();
        NavigationItems.ItemsSource = _entries;
    }

    public void Configurar(UsuarioModel usuario, IReadOnlyList<OpcionNavegacion> opciones)
    {
        EmployeeName.Text = usuario.Nombre;
        EmployeeRole.Text = usuario.Rol;
        _entries.Clear();
        for (var i = 0; i < opciones.Count; i++)
        {
            var item = opciones[i];
            _entries.Add(new NavigationEntry(item.Grupo, i == 0 || opciones[i - 1].Grupo != item.Grupo ? item.Grupo : "", item.Nombre, item.Icono, i == 0));
        }
    }

    public void Seleccionar(string modulo)
    {
        foreach (var entry in _entries) entry.Seleccionado = entry.Nombre == modulo;
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name }) NavigationRequested?.Invoke(this, name);
    }

    private void Logout_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);

    public sealed class NavigationEntry(string grupo, string encabezado, string nombre, string icono, bool seleccionado) : INotifyPropertyChanged
    {
        private bool _seleccionado = seleccionado;
        public string Grupo { get; } = grupo;
        public string Encabezado { get; } = encabezado;
        public string Nombre { get; } = nombre;
        public string Icono { get; } = icono;
        public bool Seleccionado { get => _seleccionado; set { if (_seleccionado == value) return; _seleccionado = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
