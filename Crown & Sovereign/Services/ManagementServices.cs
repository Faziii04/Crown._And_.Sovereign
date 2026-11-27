using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;

namespace Crown___Sovereign.Services;

public sealed class AdministracionService
{
    public IReadOnlyList<EmpleadoModel> Empleados(string filtro="")=>SampleData.Empleados.Where(e=>string.IsNullOrWhiteSpace(filtro)||$"{e.Nombre} {e.Cargo} {e.Email}".Contains(filtro,StringComparison.OrdinalIgnoreCase)).ToList();
    public EmpleadoModel CrearEmpleado(string nombre,string cargo,string correo,string telefono)
    {if(!NavigationService.Puede("Empleados"))throw new UnauthorizedAccessException();if(string.IsNullOrWhiteSpace(nombre)||string.IsNullOrWhiteSpace(cargo)||!correo.Contains('@'))throw new InvalidOperationException("Completa nombre, cargo y correo válido.");var item=new EmpleadoModel(SampleData.Empleados.Max(e=>e.Id)+1,nombre.Trim(),cargo.Trim(),correo.Trim(),telefono.Trim(),null);SampleData.Empleados.Add(item);Registrar("Alta de empleado",item.Nombre);return item;}
    public IReadOnlyList<UsuarioModel> Usuarios()=>SampleData.Usuarios;
    public IReadOnlyList<RolModel> Roles()=>SampleData.Roles;
    public void CambiarEstadoModulo(int id,bool activo){if(!NavigationService.Puede("Módulos y parámetros"))throw new UnauthorizedAccessException();var item=SampleData.Modulos.First(m=>m.Id==id);SampleData.Modulos[SampleData.Modulos.IndexOf(item)]=item with{Activo=activo};Registrar("Estado de módulo",$"{item.Nombre}: {activo}");}
    public void GuardarConfiguracion(string clave,string valor){if(!NavigationService.Puede("Configuración del hotel"))throw new UnauthorizedAccessException();if(string.IsNullOrWhiteSpace(valor))throw new InvalidOperationException("El valor no puede quedar vacío.");SampleData.ConfiguracionHotel[clave]=valor.Trim();Registrar("Configuración del hotel",$"{clave}: {valor}");}
    public IReadOnlyList<AuditoriaModel> Auditoria()=>SampleData.Auditoria;
    private static void Registrar(string cambio,string despues)=>AuditoriaRepositorio.Registrar(new(DateTime.Now,SesionActual.Usuario?.Usuario??"demo",cambio,"—",despues));
}

public sealed class GerenciaService
{
    public IReadOnlyList<TarifaModel> Tarifas()=>SampleData.TarifasHotel;
    public void ActualizarTarifa(int id,decimal precio){if(!NavigationService.Puede("Tarifas y temporadas"))throw new UnauthorizedAccessException();if(precio<=0)throw new InvalidOperationException("La tarifa debe ser mayor que cero.");var i=SampleData.TarifasHotel.FindIndex(t=>t.Id==id);SampleData.TarifasHotel[i]=SampleData.TarifasHotel[i] with{Precio=precio};}
    public IReadOnlyList<PaqueteModel> Paquetes()=>SampleData.Paquetes;
    public IReadOnlyList<PromocionModel> Promociones()=>SampleData.Promociones;
    public IReadOnlyList<ConvenioModel> Convenios()=>SampleData.Convenios;
    public IReadOnlyList<PoliticaModel> Politicas()=>SampleData.Politicas;
    public IReadOnlyList<ReporteModel> Reportes()=>SampleData.Reportes;
    public IReadOnlyList<MovimientoModel> Movimientos()=>SampleData.Movimientos;
    public IReadOnlyList<ModuloModel> Modulos()=>SampleData.Modulos;
    public IReadOnlyList<TareaModel> Tareas()=>SampleData.Tareas;
    public IReadOnlyList<IncidenciaModel> Incidencias()=>SampleData.Incidencias;
    public void GenerarReporte(string nombre){if(!NavigationService.Puede("Reportes"))throw new UnauthorizedAccessException();SampleData.Reportes.Insert(0,new(SampleData.Reportes.Count+1,nombre,"Operación",DateTime.Now,"Generado"));}
}
