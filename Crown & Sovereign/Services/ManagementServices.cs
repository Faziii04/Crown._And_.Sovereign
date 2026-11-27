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
    public IReadOnlyList<CanalReservaModel> CanalesReserva()=>SampleData.CanalesReserva;
    public IReadOnlyList<TemporadaModel> Temporadas()=>SampleData.Temporadas.OrderBy(item=>item.Desde).ToList();
    public IReadOnlyList<CeldaMatrizTarifa> MatrizTarifas(int canalId)
    {
        var channel=SampleData.CanalesReserva.FirstOrDefault(item=>item.Id==canalId)??throw new InvalidOperationException("Selecciona un canal válido.");
        var duplicate=SampleData.TarifasConfiguraciones.GroupBy(item=>(item.TarifaId,item.TemporadaId,item.CanalId)).FirstOrDefault(group=>group.Count()>1);if(duplicate is not null)throw new InvalidOperationException("Hay configuraciones tarifarias duplicadas. Revisa los datos antes de usar la matriz.");
        return (from season in Temporadas() from rate in SampleData.TarifasHotel let config=SampleData.TarifasConfiguraciones.SingleOrDefault(item=>item.TarifaId==rate.Id&&item.TemporadaId==season.Id&&item.CanalId==channel.Id) select new CeldaMatrizTarifa(rate.TipoHabitacion,rate.Id,season.Nombre,season.Id,config?.Precio??rate.Precio,config is null?"Tarifa base":"Configurada",config is not null,config?.Id)).ToList();
    }
    public void ActualizarTarifa(int id,decimal precio){AutorizarTarifas();ValidarPrecio(precio);var i=SampleData.TarifasHotel.FindIndex(t=>t.Id==id);if(i<0)throw new InvalidOperationException("No se encontró el tipo de habitación.");SampleData.TarifasHotel[i]=SampleData.TarifasHotel[i] with{Precio=precio};}
    public void GuardarTarifaConfiguracion(int tarifaId,int temporadaId,int canalId,decimal precio)
    {
        AutorizarTarifas();ValidarPrecio(precio);
        if(!SampleData.TarifasHotel.Any(item=>item.Id==tarifaId)||!SampleData.Temporadas.Any(item=>item.Id==temporadaId)||!SampleData.CanalesReserva.Any(item=>item.Id==canalId))throw new InvalidOperationException("La habitación, temporada o canal ya no está disponible.");
        var matches=SampleData.TarifasConfiguraciones.Select((item,index)=>(item,index)).Where(pair=>pair.item.TarifaId==tarifaId&&pair.item.TemporadaId==temporadaId&&pair.item.CanalId==canalId).ToList();if(matches.Count>1)throw new InvalidOperationException("Hay configuraciones duplicadas para esta combinación. Corrige los datos antes de editar el precio.");var i=matches.Count==0?-1:matches[0].index;
        if(i>=0)SampleData.TarifasConfiguraciones[i]=SampleData.TarifasConfiguraciones[i] with{Precio=precio};else SampleData.TarifasConfiguraciones.Add(new(SampleData.TarifasConfiguraciones.Count==0?1:SampleData.TarifasConfiguraciones.Max(item=>item.Id)+1,tarifaId,temporadaId,canalId,precio));
    }
    public TemporadaModel GuardarTemporada(int? id,string nombre,DateTime desde,DateTime hasta)
    {
        AutorizarTarifas();if(string.IsNullOrWhiteSpace(nombre))throw new InvalidOperationException("Escribe el nombre de la temporada.");if(hasta.Date<=desde.Date)throw new InvalidOperationException("La fecha final debe ser posterior a la inicial; el último día no se incluye.");
        var conflict=SampleData.Temporadas.FirstOrDefault(item=>item.Id!=id&&desde.Date<item.Hasta.Date&&item.Desde.Date<hasta.Date);if(conflict is not null)throw new InvalidOperationException($"El periodo coincide con «{conflict.Nombre}» ({conflict.Desde:dd/MM/yyyy}–{conflict.Hasta:dd/MM/yyyy}).");
        TemporadaModel result;if(id is int existing){var old=SampleData.Temporadas.FirstOrDefault(item=>item.Id==existing)??throw new InvalidOperationException("No se encontró la temporada.");var used=SampleData.TarifasConfiguraciones.Any(item=>item.TemporadaId==existing)||SampleData.Reservas.Any(item=>item.Llegada.Date<old.Hasta.Date&&item.Salida.Date>old.Desde.Date);if(used&&(old.Desde.Date!=desde.Date||old.Hasta.Date!=hasta.Date))throw new InvalidOperationException("Esta temporada ya tiene precios o reservas relacionadas. Conserva sus fechas; crea otra temporada para un periodo distinto.");result=new(existing,nombre.Trim(),desde.Date,hasta.Date);SampleData.Temporadas[SampleData.Temporadas.IndexOf(old)]=result;}else{result=new(SampleData.Temporadas.Count==0?1:SampleData.Temporadas.Max(item=>item.Id)+1,nombre.Trim(),desde.Date,hasta.Date);SampleData.Temporadas.Add(result);}return result;
    }
    public void EliminarTemporada(int id)
    {
        AutorizarTarifas();var season=SampleData.Temporadas.FirstOrDefault(item=>item.Id==id)??throw new InvalidOperationException("No se encontró la temporada.");
        if(SampleData.TarifasConfiguraciones.Any(item=>item.TemporadaId==id))throw new InvalidOperationException("No se puede eliminar: la temporada conserva precios configurados. Mantén su historial o crea una nueva temporada.");
        if(SampleData.Reservas.Any(item=>item.Llegada.Date<season.Hasta.Date&&item.Salida.Date>season.Desde.Date))throw new InvalidOperationException("No se puede eliminar: hay reservas relacionadas con este periodo. El historial de reservas y cargos se conserva.");
        SampleData.Temporadas.Remove(season);
    }
    public void ActualizarCanal(int id,string nombre,decimal comision)
    {AutorizarTarifas();if(string.IsNullOrWhiteSpace(nombre))throw new InvalidOperationException("Escribe el nombre del canal.");if(comision is <0 or >100)throw new InvalidOperationException("La comisión debe estar entre 0 % y 100 %.");if(SampleData.CanalesReserva.Any(item=>item.Id!=id&&item.Nombre.Equals(nombre.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Ya existe un canal con ese nombre.");var i=SampleData.CanalesReserva.FindIndex(item=>item.Id==id);if(i<0)throw new InvalidOperationException("No se encontró el canal.");var current=SampleData.CanalesReserva[i];if(!current.Nombre.Equals(nombre.Trim(),StringComparison.OrdinalIgnoreCase)&&SampleData.Reservas.Any(item=>item.Canal.Equals(current.Nombre,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Este canal ya se usa en reservas. Conserva el nombre para mantener su historial y cambia solo la comisión.");SampleData.CanalesReserva[i]=current with{Nombre=nombre.Trim(),Comision=comision};}
    public CanalReservaModel CrearCanal(string nombre,decimal comision)
    {AutorizarTarifas();if(string.IsNullOrWhiteSpace(nombre)||SampleData.CanalesReserva.Any(item=>item.Nombre.Equals(nombre.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Indica un nombre de canal único.");if(comision is <0 or >100)throw new InvalidOperationException("La comisión debe estar entre 0 % y 100 %.");var model=new CanalReservaModel(SampleData.CanalesReserva.Max(item=>item.Id)+1,nombre.Trim(),comision);SampleData.CanalesReserva.Add(model);return model;}
    public IReadOnlyList<AjusteTarifarioPreviewModel> PrevisualizarAjuste(bool tarifasBase,decimal valor,bool porcentaje,IEnumerable<int>? temporadaIds=null)
    {
        AutorizarTarifas();var rows=new List<AjusteTarifarioPreviewModel>();
        if(tarifasBase)rows.AddRange(SampleData.TarifasHotel.Select(item=>new AjusteTarifarioPreviewModel($"{item.TipoHabitacion} · Tarifa base",item.Precio,decimal.Round(porcentaje?item.Precio*(1+valor/100m):item.Precio+valor,2))));
        else{var selected=temporadaIds?.ToHashSet();foreach(var config in SampleData.TarifasConfiguraciones.Where(item=>selected is null||selected.Contains(item.TemporadaId))){var rate=SampleData.TarifasHotel.First(item=>item.Id==config.TarifaId);var season=SampleData.Temporadas.First(item=>item.Id==config.TemporadaId);var channel=SampleData.CanalesReserva.First(item=>item.Id==config.CanalId);rows.Add(new($"{rate.TipoHabitacion} · {season.Nombre} · {channel.Nombre}",config.Precio,decimal.Round(porcentaje?config.Precio*(1+valor/100m):config.Precio+valor,2)));}}
        return rows;
    }
    public void AplicarAjuste(bool tarifasBase,decimal valor,bool porcentaje,IEnumerable<int>? temporadaIds=null)
    {
        AutorizarTarifas();var selected=temporadaIds?.ToHashSet();var rateChanges=tarifasBase?SampleData.TarifasHotel.Select(item=>item with{Precio=decimal.Round(porcentaje?item.Precio*(1+valor/100m):item.Precio+valor,2)}).ToList():[];var configChanges=tarifasBase?[]:SampleData.TarifasConfiguraciones.Where(item=>selected is null||selected.Contains(item.TemporadaId)).Select(item=>item with{Precio=decimal.Round(porcentaje?item.Precio*(1+valor/100m):item.Precio+valor,2)}).ToList();
        if(rateChanges.Any(item=>item.Precio<=0)||configChanges.Any(item=>item.Precio<=0))throw new InvalidOperationException("El ajuste produciría precios no válidos. No se guardó ningún cambio.");
        if(tarifasBase){foreach(var item in rateChanges){var i=SampleData.TarifasHotel.FindIndex(old=>old.Id==item.Id);SampleData.TarifasHotel[i]=item;}}else{foreach(var item in configChanges){var i=SampleData.TarifasConfiguraciones.FindIndex(old=>old.Id==item.Id);SampleData.TarifasConfiguraciones[i]=item;}}
    }
    public void CopiarTemporada(int sourceId,int targetId,bool reemplazar)
    {
        AutorizarTarifas();if(sourceId==targetId)throw new InvalidOperationException("Elige temporadas distintas.");if(!SampleData.Temporadas.Any(item=>item.Id==sourceId)||!SampleData.Temporadas.Any(item=>item.Id==targetId))throw new InvalidOperationException("La temporada de origen o destino ya no existe.");
        var source=SampleData.TarifasConfiguraciones.Where(item=>item.TemporadaId==sourceId).ToList();if(source.Count==0)throw new InvalidOperationException("La temporada de origen no tiene precios configurados.");
        if(source.GroupBy(item=>(item.TarifaId,item.CanalId)).Any(group=>group.Count()>1)||SampleData.TarifasConfiguraciones.Where(item=>item.TemporadaId==targetId).GroupBy(item=>(item.TarifaId,item.CanalId)).Any(group=>group.Count()>1))throw new InvalidOperationException("Hay configuraciones duplicadas en el origen o el destino. Corrige esos datos antes de copiar.");
        var operations=source.Select(item=>(Source:item,Target:SampleData.TarifasConfiguraciones.FirstOrDefault(target=>target.TarifaId==item.TarifaId&&target.TemporadaId==targetId&&target.CanalId==item.CanalId))).ToList();
        foreach(var operation in operations){if(operation.Target is null)SampleData.TarifasConfiguraciones.Add(new(SampleData.TarifasConfiguraciones.Count==0?1:SampleData.TarifasConfiguraciones.Max(target=>target.Id)+1,operation.Source.TarifaId,targetId,operation.Source.CanalId,operation.Source.Precio));else if(reemplazar){var index=SampleData.TarifasConfiguraciones.IndexOf(operation.Target);SampleData.TarifasConfiguraciones[index]=operation.Target with{Precio=operation.Source.Precio};}}
    }
    private static void AutorizarTarifas(){if(!NavigationService.Puede("Tarifas y temporadas"))throw new UnauthorizedAccessException("Solo el Gerente puede modificar las tarifas comerciales.");}
    private static void ValidarPrecio(decimal price){if(price<=0)throw new InvalidOperationException("El precio debe ser mayor que cero.");}
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
