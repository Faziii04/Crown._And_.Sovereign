using Crown___Sovereign.Infrastructure;
using Crown___Sovereign.Data;
using Crown___Sovereign.Models;
using Crown___Sovereign.Services;
var passed=0; var failed=0;
void Check(string name,bool value){if(value){passed++;Console.WriteLine($"PASS {name}");}else{failed++;Console.WriteLine($"FAIL {name}");}}
var service=new SupervisionOperativaService();
var snapshot=service.Obtener("Hoy");
Check("exclusive room categories reconcile",snapshot.HabitacionesTotales==snapshot.HabitacionesListas+snapshot.HabitacionesOcupadas+snapshot.HabitacionesPreparacion+snapshot.HabitacionesBloqueadas);
Check("current room listing includes all rooms",snapshot.Habitaciones.Count==SampleData.Habitaciones.Count);
var room=SampleData.Habitaciones[0]; var savedBlock=room.Bloqueada; var savedClean=room.Limpieza; room.Bloqueada=true; room.Limpieza="Limpia";
var blocked=new HabitacionService().ObtenerEstadoOperativo(room.Id);
Check("clean but blocked room is not ready",blocked.Bloqueada&&blocked.Clasificacion=="Bloqueada"); room.Bloqueada=false;
var incident=new IncidenciaModel{Id=7777,Habitacion=room.Id,Descripcion="test",BloqueaHabitacion=true,Estado="Abierta",FechaReporte=DateTime.Now}; SampleData.Incidencias.Add(incident);
var incidentOnly=new HabitacionService().ObtenerEstadoOperativo(room.Id);
Check("incident blockage request is not itself an actual blockage",!incidentOnly.Bloqueada&&incidentOnly.RequiereRevisionOperativa);
SampleData.Incidencias.Remove(incident); room.Bloqueada=savedBlock; room.Limpieza=savedClean;
Check("arrival risk counts distinct reservations",snapshot.Indicadores.Any(x=>x.Titulo=="Ingresos en riesgo"));
Check("unassigned workload is represented",snapshot.Carga.Any(x=>x.Responsable=="Sin asignar"));
Check("read-only refresh does not create records",SampleData.Tareas.Count==snapshot.Tareas.Count(x=>SampleData.Tareas.Any(t=>t.Id==x.Id)));
var savedStock=SampleData.Productos[0].Stock; SampleData.Productos[0].Stock=0;
var low=service.Obtener("Hoy"); Check("exhausted stock is flagged and linked services shown",low.Productos.Any(x=>x.Id==SampleData.Productos[0].Id&&x.Estado=="Agotado")); SampleData.Productos[0].Stock=savedStock;
var savedNow=SampleData.Habitaciones[0].Estado; var prev=service.Obtener("Mes anterior");
Check("historical period keeps current room state distinct",prev.BasePeriodo.Contains("estado actual",StringComparison.OrdinalIgnoreCase));
Check("custom date range is exposed",service.Obtener("Periodo personalizado",DateTime.Today.AddDays(-9),DateTime.Today.AddDays(-2)).Periodo=="Periodo personalizado");
Check("priority incident appears in supervision",snapshot.Incidencias.Any(x=>x.Prioridad is "Alta" or "Urgente"));
Check("open services remain visible independent of selected period",service.Obtener("Mes anterior").Servicios.Any(x=>x.Tipo=="Solicitud"&&x.Estado is not ("Completada" or "Cancelada")));
Check("resource return warning derives from delivered allocation and stay end",snapshot.Recursos.All(x=>!x.DevolucionPendiente||x.Estado=="ENTREGADO"));
var staff=SampleData.Empleados.First(x=>x.Cargo=="Servicios");
var appointmentServiceModel=SampleData.EmpleadosServicios.First(x=>x.Activo);
var serviceId=appointmentServiceModel.ServicioId;
var baseId=SampleData.Citas.Max(x=>x.Id)+100;
var overlapStart=DateTime.Today.AddHours(2);
var firstAppointment=new CitaServicioModel(baseId,1,serviceId,overlapStart,overlapStart.AddHours(1),staff.Nombre,"Confirmada");
var secondAppointment=new CitaServicioModel(baseId+1,2,serviceId,overlapStart.AddMinutes(20),overlapStart.AddMinutes(80),staff.Nombre,"Confirmada");
var cancelledAppointment=new CitaServicioModel(baseId+2,3,serviceId,overlapStart,overlapStart.AddHours(1),staff.Nombre,"Cancelada");
SampleData.Citas.AddRange([firstAppointment,secondAppointment,cancelledAppointment]);
var schedule=service.Obtener("Hoy").Servicios;
Check("overlapping active appointments are flagged",schedule.Where(x=>x.Id is var id && (id==baseId||id==baseId+1)).All(x=>x.Conflicto));
Check("cancelled appointment does not create conflict",schedule.First(x=>x.Id==baseId+2).Conflicto==false);
SampleData.Citas.RemoveAll(x=>x.Id>=baseId&&x.Id<=baseId+2);
var manager=SampleData.Usuarios.First(x=>x.Rol=="Gerente"); SesionActual.Iniciar(manager);
var reports = new ReporteService();
var reportCatalogWorks=true;
foreach(var definition in ReporteService.Catalogo)
    try { reports.Construir(definition, ReporteService.ResolverPeriodo(definition.Id,"Últimos 30 días")); }
    catch(Exception ex) { reportCatalogWorks=false; Console.WriteLine($"REPORT ERROR {definition.Id}: {ex.Message}"); }
Check("all report catalog entries build a result",reportCatalogWorks);
var period = ReporteService.ResolverPeriodo("ocupacion", "Últimos 7 días");
var occupancyReport = reports.Construir(ReporteService.Catalogo.First(x=>x.Id=="ocupacion"), period);
Check("occupancy report reconciles physical room nights", occupancyReport.Indicadores.First(x=>x.Nombre=="Noches habitación instaladas").ValorNumerico == SampleData.Habitaciones.Count * 7m);
Check("occupancy report never exceeds installed capacity", occupancyReport.Indicadores.First(x=>x.Nombre=="Noches ocupadas").ValorNumerico <= occupancyReport.Indicadores.First(x=>x.Nombre=="Noches habitación instaladas").ValorNumerico);
var recentBefore = ReporteService.Recientes.Count;
var generated = reports.Generar(ReporteService.ResolverPeriodo("ingresos", "Este mes"));
Check("report generation returns a preview and records recent output", generated.Count > recentBefore && generated[0].Id=="ingresos");
var invalidRange=false; try { reports.Generar(new("ingresos","custom",DateTime.Today,DateTime.Today.AddDays(-1))); } catch(InvalidOperationException) { invalidRange=true; }
Check("report rejects reversed date ranges", invalidRange);
var hotelPolicies = new PoliticaHotelService();
var servicePolicy = hotelPolicies.PoliticaServicioActual;
var auditCount=SampleData.HistorialPoliticas.Count;
var policySnapshot=PoliticaHotelService.SnapshotActualServicio();
Check("appointment policy snapshot round trips", PoliticaHotelService.LeerSnapshot(policySnapshot)==servicePolicy);
var policySim=hotelPolicies.SimularServicio(DateTime.Today.AddDays(1),DateTime.Today, false, 100m, false, false);
Check("service policy simulation does not mutate policy or audit", SampleData.PoliticasServicio.FirstOrDefault()==servicePolicy && SampleData.HistorialPoliticas.Count==auditCount && policySim.Penalizacion>=0);
var fixedPenalty=PoliticaHotelService.CalcularPenalizacion("Monto fijo",25m,100m);
var percentagePenalty=PoliticaHotelService.CalcularPenalizacion("Porcentaje",25m,100m);
Check("service penalties calculate fixed and percentage values",fixedPenalty==25m&&percentagePenalty==25m);
var invalidPenalty=false; try { PoliticaHotelService.CalcularPenalizacion("Porcentaje",101m,100m); } catch(InvalidOperationException) { invalidPenalty=true; }
Check("service policy rejects percentages above 100",invalidPenalty);
var notificationBefore=SampleData.Notificaciones.Count;
var alert=new SupervisionAlerta("Requiere atención","Habitación 101","Limpieza pendiente","Ingreso previsto","Revisar preparación",DateTime.Today,"Habitaciones",1);
service.NotificarSituacion(alert,"Revisar preparación de la habitación 101.");
var created=SampleData.Notificaciones.FirstOrDefault(x=>x.Mensaje=="Revisar preparación de la habitación 101.");
Check("manager escalation creates an in-app notification for operations",SampleData.Notificaciones.Count==notificationBefore+1&&created?.Destino=="Encargado de servicios"&&created.ModuloDestino=="Limpieza");
if(created is not null) SampleData.Notificaciones.Remove(created);
SesionActual.Iniciar(SampleData.Usuarios.First(x=>x.Rol=="Recepcionista"));
var denied=false; try { service.NotificarSituacion(alert,"No autorizado"); } catch(UnauthorizedAccessException) { denied=true; }
Check("notification action enforces manager permission",denied);
SesionActual.Cerrar();

Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed==0?0:1;
