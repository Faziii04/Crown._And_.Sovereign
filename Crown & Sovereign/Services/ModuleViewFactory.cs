using System.Windows.Controls;
using Crown___Sovereign.Views.UserControls.Administracion.Dashboard;
using Crown___Sovereign.Views.UserControls.Gerencia.Dashboard;
using Crown___Sovereign.Views.UserControls.Operaciones.Dashboard;
using Crown___Sovereign.Views.UserControls.Recepcion.Dashboard;
using Crown___Sovereign.Views.UserControls.Recepcion.Reservas;
using Crown___Sovereign.Views.UserControls.Recepcion.Habitaciones;
using Crown___Sovereign.Views.UserControls.Recepcion.Huespedes;
using Crown___Sovereign.Views.UserControls.Recepcion.Estancias;
using Crown___Sovereign.Views.UserControls.Recepcion.Caja;
using Crown___Sovereign.Views.UserControls.Recepcion.Servicios;
using Crown___Sovereign.Views.UserControls.Operaciones.AtencionServicios;
using Crown___Sovereign.Views.UserControls.Operaciones.AgendaServicios;
using Crown___Sovereign.Views.UserControls.Operaciones.CatalogoServicios;
using Crown___Sovereign.Views.UserControls.Operaciones.Consumos;
using Crown___Sovereign.Views.UserControls.Operaciones.Inventario;
using Crown___Sovereign.Views.UserControls.Operaciones.Limpieza;
using Crown___Sovereign.Views.UserControls.Operaciones.Mantenimiento;
using Crown___Sovereign.Views.UserControls.Operaciones.ObjetosOlvidados;
using Crown___Sovereign.Views.UserControls.Administracion.Empleados;
using Crown___Sovereign.Views.UserControls.Administracion.UsuariosRoles;
using Crown___Sovereign.Views.UserControls.Administracion.Configuracion;
using Crown___Sovereign.Views.UserControls.Administracion.Sistema;
using Crown___Sovereign.Views.UserControls.Gerencia.Comercial;
using Crown___Sovereign.Views.UserControls.Gerencia.Finanzas;
using Crown___Sovereign.Views.UserControls.Gerencia.Supervision;

namespace Crown___Sovereign.Services;

/// <summary>Resuelve cada opción de navegación a un control raíz concreto. No genera interfaces.</summary>
public sealed class ModuleViewFactory
{
    public UserControl Crear(string modulo, string rol)
    {
        if (modulo == "Reservas" && rol == "Recepcionista") return new ReservasControl();
        if (modulo == "Habitaciones" && rol == "Recepcionista") return new HabitacionesControl();
        if (modulo == "Habitaciones y pisos" && rol == "Administrador") return new HabitacionesControl(administracion: true);
        if (modulo == "Huéspedes" && rol == "Recepcionista") return new HuespedesControl();
        if (modulo == "Estancias" && rol == "Recepcionista") return new EstanciasControl();
        if (modulo == "Caja y facturación" && rol == "Recepcionista") return new CajaControl();
        if (modulo == "Servicios" && rol == "Recepcionista") return new ServiciosRecepcionControl();
        if (modulo == "Atención de servicios" && rol == "Encargado de servicios") return new AtencionServiciosControl();
        if (modulo == "Agenda de servicios" && rol == "Encargado de servicios") return new AgendaServiciosControl();
        if (modulo == "Catálogo de servicios" && rol is "Encargado de servicios" or "Gerente") return new CatalogoServiciosControl();
        if (modulo == "Catálogo de servicios" && rol == "Gerente") return new CatalogoServiciosControl();
        if (modulo == "Consumos" && rol == "Encargado de servicios") return new ConsumosControl();
        if (modulo == "Productos e inventario" && rol == "Encargado de servicios") return new InventarioControl();
        if (modulo == "Limpieza" && rol == "Encargado de servicios") return new LimpiezaControl();
        if (modulo == "Mantenimiento" && rol == "Encargado de servicios") return new MantenimientoControl();
        if (modulo == "Objetos olvidados" && rol == "Encargado de servicios") return new ObjetosOlvidadosControl();
        if (modulo == "Empleados" && rol == "Administrador") return new EmpleadosControl();
        if (modulo == "Usuarios y roles" && rol == "Administrador") return new UsuariosRolesControl();
        if (modulo == "Configuración del hotel" && rol == "Administrador") return new ConfiguracionHotelControl();
        if (modulo == "Módulos y parámetros" && rol == "Administrador") return new ModulosParametrosControl();
        if (modulo == "Auditoría" && rol == "Administrador") return new AuditoriaControl();
        if (modulo == "Tarifas y temporadas" && rol == "Gerente") return new TarifasTemporadasControl();
        if (modulo == "Paquetes y promociones" && rol == "Gerente") return new PaquetesPromocionesControl();
        if (modulo == "Convenios" && rol == "Gerente") return new ConveniosControl();
        if (modulo == "Finanzas" && rol == "Gerente") return new FinanzasControl();
        if (modulo == "Operaciones" && rol == "Gerente") return new OperacionesControl();
        if (modulo == "Reportes" && rol == "Gerente") return new ReportesControl();
        if (modulo == "Políticas del hotel" && rol == "Gerente") return new PoliticasHotelControl();
        if (modulo == "Inicio") return rol switch
        {
            "Administrador" => new AdministracionDashboardControl(),
            "Recepcionista" => new RecepcionDashboardControl(),
            "Encargado de servicios" => new OperacionesDashboardControl(),
            "Gerente" => new GerenciaDashboardControl(),
            _ => throw new InvalidOperationException("El rol no tiene un panel principal asignado.")
        };
        throw new KeyNotFoundException($"No existe un control principal registrado para '{modulo}'.");
    }
}
