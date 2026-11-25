using Crown___Sovereign.Data;
using System.Windows.Controls;
using Crown___Sovereign.Services;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Mantenimiento;
public partial class OrdenesMantenimientoControl : UserControl{public OrdenesMantenimientoControl(){InitializeComponent();OrdersGrid.ItemsSource=new MantenimientoService().ListarOrdenes().Select(o=>{var incident=SampleData.Incidencias.First(i=>i.Id==o.IncidenciaId);return new{Incidencia=incident.Descripcion,Habitacion=SampleData.Habitaciones.First(r=>r.Id==incident.Habitacion).Numero,o.AsignadoA,o.FechaEstimada,o.Estado};}).ToList();}}
