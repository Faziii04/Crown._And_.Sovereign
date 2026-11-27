using System.Windows.Controls;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;

public partial class InformacionServicioControl : UserControl
{
    public InformacionServicioControl() => InitializeComponent();
    public void Mostrar(ServicioModel service)
    {
        NameText.Text = service.Nombre; CategoryText.Text = service.Categoria;
        DescriptionText.Text = string.IsNullOrWhiteSpace(service.Descripcion) ? "Sin descripción registrada." : service.Descripcion;
        var children = SampleData.Servicios.Where(item => item.PadreId == service.Id).OrderBy(item => item.Nombre).ToList();
        ChildrenList.ItemsSource = children.Count == 0 ? new[] { "Este servicio no agrupa subservicios." } : children.Select(item => $"{item.Nombre} · {(item.Activo ? "Activo" : "Inactivo")} · {(item.Reservable ? "Reservable" : "Agrupación")}").ToList();
    }
}
