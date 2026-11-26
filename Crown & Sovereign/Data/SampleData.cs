using Crown___Sovereign.Models;

namespace Crown___Sovereign.Data;

/// <summary>Datos ficticios compartidos en memoria. Las cuentas demo deben sustituirse antes de producción.</summary>
public static class SampleData
{
    public static readonly List<UsuarioModel> Usuarios =
    [new(1,"admin","demo123","María Fernanda Rojas","Administrador"), new(2,"recepcion","demo123","Diego Salvatierra","Recepcionista"), new(3,"servicios","demo123","Lucía Méndez","Encargado de servicios"), new(4,"gerente","demo123","Andrés Quiroga","Gerente"), new(5,"recepcion2","demo123","Andrea Vargas","Recepcionista")];
    public static readonly List<EmpleadoModel> Empleados = [];
    public static readonly List<RolModel> Roles = [new(1,"Administrador","Configuración y usuarios"),new(2,"Recepcionista","Reservas, estancias y caja"),new(3,"Encargado de servicios","Servicios e instalaciones"),new(4,"Gerente","Supervisión y reportes")];
    public static readonly List<TemporadaModel> Temporadas = [new(1,"Temporada alta",DateTime.Today,DateTime.Today.AddMonths(3)),new(2,"Temporada regular",DateTime.Today.AddMonths(3),DateTime.Today.AddMonths(9))];
    public static readonly List<TarifaModel> TarifasHotel = [];
    public static readonly List<PaqueteModel> Paquetes = [new(1,"Bienestar","Alojamiento, spa y desayuno",720,true),new(2,"Desayuno diario","Alojamiento y desayuno",560,true),new(3,"Escapada Andina","Dos noches y traslado",1480,true)];
    public static readonly List<PromocionModel> Promociones = [new(1,"Reserva anticipada","Porcentaje",12,DateTime.Today.AddDays(-20),DateTime.Today.AddDays(60)),new(2,"Estadía extendida","Noches",1,DateTime.Today,DateTime.Today.AddMonths(4))];
    public static readonly List<ConvenioModel> Convenios = [new(1,"Fundación Horizonte","Corporativo",DateTime.Today.AddMonths(-2),DateTime.Today.AddMonths(10),true),new(2,"Andes Travel","Agencia",DateTime.Today.AddMonths(-5),DateTime.Today.AddMonths(1),true)];
    public static readonly List<PoliticaModel> Politicas = [new(1,"Check-in","Ingreso estándar del huésped","15:00"),new(2,"Check-out","Salida estándar del huésped","11:00"),new(3,"Cancelación","Plazo para cancelar sin penalidad","48 horas"),new(4,"Mascotas","Admisión sujeta a habitaciones asignadas","Consultar")];
    public static readonly List<ModuloModel> Modulos = [];
    public static readonly List<ReporteModel> Reportes = [];
    public static readonly List<CitaServicioModel> Citas = [];
    public static readonly List<SolicitudServicioModel> SolicitudesServicio = [];
    public static readonly List<UsoServicioModel> UsosServicio = [];
    public static readonly List<TrasladoModel> Traslados = [];
    public static readonly List<ObjetoOlvidadoModel> ObjetosOlvidados = [];
    public static readonly string[] Tipos = ["Estándar", "Doble", "Superior", "Junior suite", "Suite"];
    public static readonly List<HabitacionModel> Habitaciones = [];
    public static readonly List<HuespedModel> Huespedes = [];
    public static readonly List<ReservaModel> Reservas = [];
    public static readonly List<GrupoReservaModel> GruposReservas = [];
    public static readonly List<ListaEsperaModel> ListaEspera = [];
    public static readonly List<ReservaHuespedModel> ReservaHuespedes = [];
    public static readonly List<ReservaHabitacionModel> ReservaHabitaciones = [];
    public static readonly List<ReservaHabitacionHuespedModel> ReservaHabitacionHuespedes = [];
    public static readonly List<ReservaPaqueteModel> ReservaPaquetes = [];
    public static readonly List<PaqueteServicioModel> PaquetesServicios = [];
    public static readonly List<ReservaPaqueteServicioModel> ReservaPaqueteServicios = [];
    public static readonly List<ServicioProductoModel> ServiciosProductos = [];
    public static readonly List<ReservaPaqueteServicioProductoModel> ReservaPaqueteServicioProductos = [];
    public static readonly List<ReservaServicioModel> ServiciosReserva = [];
    public static readonly List<ReservaServicioAdquiridoModel> ServiciosAdquiridos = [];
    public static readonly List<RecursoAdicionalModel> RecursosAdicionales = [];
    public static readonly List<ReservaRecursoModel> ReservaRecursos = [];
    public static readonly List<SolicitudEspecialModel> SolicitudesEspeciales = [];
    public static readonly List<RecursoReservaModel> RecursosReserva = [];
    public static readonly List<ContactoListaEsperaModel> ContactosListaEspera = [];
    public static readonly List<EstanciaModel> Estancias = [];
    public static readonly List<FolioModel> Folios = [];
    public static readonly List<LlaveModel> Llaves = [];
    public static readonly List<ConsumoModel> Consumos = [];
    public static readonly List<MovimientoInventarioModel> MovimientosInventario = [];
    public static readonly List<ServicioModel> Servicios = [];
    public static readonly List<ProductoModel> Productos = [];
    public static readonly List<TareaModel> Tareas = [];
    public static readonly List<IncidenciaModel> Incidencias = [];
    public static readonly List<InspeccionModel> Inspecciones = [];
    public static readonly List<OrdenMantenimientoModel> OrdenesMantenimiento = [];
    public static readonly List<NotificacionModel> Notificaciones = [];
    public static readonly List<CambioHabitacionModel> CambiosHabitacion = [];
    public static readonly List<ConsentimientoHistorialModel> ConsentimientosHistorial = [];
    public static readonly List<CargoModel> Cargos = [];
    public static readonly List<MovimientoModel> Movimientos = [];
    public static readonly List<CierreCajaModel> CierresCaja = [];
    public static readonly List<FacturaModel> Facturas = [];
    public static readonly List<TurnoCajaModel> TurnosCaja = [];
    public static readonly List<MovimientoCajaModel> MovimientosCaja = [];
    public static readonly List<PagoModel> Pagos = [];
    public static readonly List<DevolucionModel> Devoluciones = [];
    public static readonly List<AnulacionModel> Anulaciones = [];
    public static readonly List<MetodoPagoModel> MetodosPago = [new(1,"Efectivo",true,true),new(2,"Tarjeta",true),new(3,"Transferencia",true)];
    public static readonly List<ImpuestoModel> Impuestos = [];
    public static readonly List<AuditoriaModel> Auditoria = [];
    public static readonly Dictionary<int, decimal> Tarifas = new();
    public static decimal AperturaCaja = 1200m;
    public static bool CajaAbierta = true;
    public static readonly Dictionary<string,string> ConfiguracionHotel = new()
    {
        ["Nombre"]="Crown & Sovereign",["Dirección"]="Av. Arce · La Paz",["Categoría"]="5 estrellas",["Teléfono"]="+591 2 200 0000",["Correo"]="reservas@crownhotel.bo"
    };
    static SampleData() => Inicializar();
    private static void Inicializar()
    {
        var random = new Random(2026); var nombres = new[] { "Camila", "Mateo", "Valentina", "Santiago", "Isabella", "Gabriel", "Lucía", "Nicolás", "Sofía", "Tomás", "Mariana", "Joaquín" };
        var apellidos = new[] { "Pérez", "Gutiérrez", "Molina", "Rivera", "Vargas", "Méndez", "Suárez", "Rojas", "Flores", "Quiroga" };
        for (int p = 1; p <= 5; p++) for (int n = 1; n <= 24; n++)
        {
            int i = (p - 1) * 24 + n; bool occupied = i <= 80; var guest = (string?)null;
            Habitaciones.Add(new() { Id=i, Numero=$"{p}{n:00}", Piso=p, Tipo=Tipos[(n-1)%5], Capacidad=((n-1)%5<2?2:((n-1)%5==4?4:3)), Estado=occupied?"Ocupada":"Disponible", Limpieza=occupied?"Limpia":i%11==0?"Pendiente de limpieza":"Limpia", Bloqueada=i%37==0, Huesped=guest, Comodidades=(n%4==0?"Jacuzzi · Balcón · Minibar · ":"Minibar · ")+"Wi-Fi · TV · Aire acondicionado" });
            Tarifas[i] = 420m + ((n-1)%5)*180m;
        }
        for (int i = 1; i <= 250; i++) Huespedes.Add(new() { Id=i, Nombre=$"{nombres[i%nombres.Length]} {apellidos[(i*3)%apellidos.Length]}", Documento=$"{6400000+i}", Email=$"huesped{i}@correo.bo", Telefono=$"+591 7{random.Next(1000000,9999999)}", Habitacion=(i<=80?i:0), Paquete=i%4==0?"Bienestar":i%3==0?"Desayuno diario":"Sin paquete", RolEnReserva=i%4==1?"Acompañante":"Principal" });
        Huespedes[16].ConsentimientoDatos = false;
        Huespedes[33].ConsentimientoDatos = false;
        Huespedes[49].Email = "";
        Huespedes[74].Telefono = "";
        for(int i=1;i<=80;i++) Habitaciones[i-1].Huesped=Huespedes[i-1].Nombre;
        for (int i=1;i<=180;i++)
        {
            var active=i<=80;
            var futureIndex=i-81;
            var room=Habitaciones[active?i-1:80+futureIndex%40];
            var batch=active?0:futureIndex/40;
            var arrival=active?DateTime.Today.AddDays(-1):futureIndex<3?DateTime.Today:DateTime.Today.AddDays(4+batch*15+futureIndex%4);
            var departure=active?(i==5||i%20==0?DateTime.Today:DateTime.Today.AddDays(2)):arrival.AddDays(2+futureIndex%4);
            int? grupoId=!active&&futureIndex<40?1:!active&&futureIndex<68?2:!active&&futureIndex<96?3:null;
            var reservation=new ReservaModel{Id=3000+i,Huesped=Huespedes[i-1].Nombre,Habitacion=room.Id,Llegada=arrival,Salida=departure,Estado=active?"En estancia":i%13==0?"Pendiente":"Confirmada",Canal=i%4==0?"Agencia":"Directa",Paquete=Huespedes[i-1].Paquete,GrupoId=grupoId,Personas=1};
            Reservas.Add(reservation);
            ReservaHabitaciones.Add(new(i,reservation.Id,room.Id));
            ReservaHuespedes.Add(new(reservation.Id,Huespedes[i-1].Id,true));
            ReservaHabitacionHuespedes.Add(new(i,reservation.Id,Huespedes[i-1].Id));
            if (Paquetes.FirstOrDefault(package => package.Nombre == reservation.Paquete) is { } purchasedPackage)
                ReservaPaquetes.Add(new(reservation.Id, purchasedPackage.Id, 1, purchasedPackage.Precio));
        }
        for (int i=1;i<=80;i++)
        {
            Estancias.Add(new(i,3000+i,i,DateTime.Today.AddDays(-1),Reservas.First(r=>r.Id==3000+i).Salida,"Activa"));
            Folios.Add(new(i,i,0,"Abierto"));
            Estancias[^1] = Estancias[^1] with { FolioId = i };
            Llaves.Add(new(i*2,i,$"CS-{Habitaciones[i-1].Numero}-A","Activa"));
            Llaves.Add(new(i*2+1,i,$"CS-{Habitaciones[i-1].Numero}-B","Activa"));
        }
        // Grupo de muestra: el huésped 250 comparte la reserva y habitación 101 como acompañante del huésped 1.
        Huespedes[249].Habitacion = 1;
        Huespedes[249].RolEnReserva = "Acompañante";
        ReservaHuespedes.Add(new(3001, 250, false));
        ReservaHabitacionHuespedes.Add(new(1, 3001, 250));
        var historicalReservationId = 6000;
        var historicalStayId = 1000;
        var roomAssignmentId = 1000;
        for (var guestId = 1; guestId <= 65; guestId++)
        {
            for (var visit = 0; visit < 2; visit++)
            {
                var arrival = DateTime.Today.AddDays(-(150 + guestId + visit * 180));
                var departure = arrival.AddDays(2 + guestId % 4);
                var room = Habitaciones[(guestId + visit * 23 - 1) % Habitaciones.Count];
                var reservationId = historicalReservationId++;
                var reservationRoomId = roomAssignmentId++;
                Reservas.Add(new ReservaModel { Id = reservationId, Huesped = Huespedes[guestId - 1].Nombre, Habitacion = room.Id, Llegada = arrival, Salida = departure, Estado = "Finalizada", Paquete = Huespedes[guestId - 1].Paquete, Personas = 1 });
                ReservaHabitaciones.Add(new(reservationRoomId, reservationId, room.Id));
                ReservaHuespedes.Add(new(reservationId, guestId, true));
                ReservaHabitacionHuespedes.Add(new(reservationRoomId, reservationId, guestId));
                var stayId = historicalStayId++;
                Estancias.Add(new EstanciaModel(stayId, reservationId, guestId, arrival, departure, "Finalizada") { FolioId = stayId });
                Folios.Add(new(stayId, stayId, 0, "Cerrado"));
            }
        }
        GruposReservas.AddRange([new(1,"Congreso de turismo Andino",DateTime.Today.AddDays(-12),DateTime.Today.AddDays(4),DateTime.Today.AddDays(8)),new(2,"Familia Valdivia",DateTime.Today.AddDays(-4),DateTime.Today.AddDays(19),DateTime.Today.AddDays(23)),new(3,"Equipo Fundación Horizonte",DateTime.Today.AddDays(-2),DateTime.Today.AddDays(34),DateTime.Today.AddDays(38))]);
        ListaEspera.AddRange([new(1,Huespedes[200].Nombre,DateTime.Today.AddDays(2),DateTime.Today.AddDays(5),"Suite","Pendiente",201,2,1),new(2,Huespedes[201].Nombre,DateTime.Today.AddDays(3),DateTime.Today.AddDays(6),"Doble","Pendiente",202,2,1),new(3,Huespedes[202].Nombre,DateTime.Today.AddDays(4),DateTime.Today.AddDays(7),"Superior","Contactar",203,1,1)]);
        var staffRoles=new[]{"Recepción","Limpieza","Mantenimiento","Servicios","Administración"};
        for(int i=1;i<=25;i++) Empleados.Add(new(i,$"{nombres[i%nombres.Length]} {apellidos[(i*3)%apellidos.Length]}",staffRoles[i%staffRoles.Length],$"empleado{i}@crownhotel.bo",$"+591 7{random.Next(1000000,9999999)}",i<=4?Usuarios[i-1].Rol:null));
        for(int i=0;i<Tipos.Length;i++) TarifasHotel.Add(new(i+1,Tipos[i],"Temporada regular",420m+i*180m,DateTime.Today,DateTime.Today.AddMonths(6)));
        string[] modules=["Reservas","Habitaciones","Huéspedes","Estancias","Caja y facturación","Servicios","Atención de servicios","Agenda de servicios","Catálogo de servicios","Consumos","Productos e inventario","Limpieza","Mantenimiento","Objetos olvidados","Finanzas","Reportes"];
        for(int i=0;i<modules.Length;i++) Modulos.Add(new(i+1,modules[i],$"Módulo operativo · {modules[i]}",true));
        Reportes.AddRange([new(1,"Ocupación por piso","Operaciones",DateTime.Today.AddDays(-1),"Disponible"),new(2,"Ingresos y pagos","Finanzas",DateTime.Today.AddDays(-2),"Disponible"),new(3,"Uso de servicios","Servicios",DateTime.Today.AddDays(-3),"Disponible")]);
        var serviceSeeds = new (string Name,string Category,int? Parent,decimal Price,bool Appointment,int Duration,string Provider,bool Bookable,bool Transfer,string Description)[]
        {
            ("Spa","Bienestar",null,0,false,0,"Interno",false,false,"Área de spa y bienestar."),
            ("Sauna","Bienestar",1,65,false,0,"Interno",true,false,"Acceso a sauna."),
            ("Masaje relajante","Bienestar",1,155,true,45,"Spa Kallpa",true,false,"Sesión de masaje orientada a la relajación."),
            ("Masaje terapéutico","Bienestar",1,180,true,60,"Spa Kallpa",true,false,"Sesión de masaje terapéutico."),
            ("Gimnasio","Gimnasio",null,0,false,0,"Interno",false,false,"Área de gimnasio."),
            ("Acceso al gimnasio","Gimnasio",5,0,false,0,"Interno",true,false,"Acceso a las instalaciones del gimnasio."),
            ("Entrenador personal","Gimnasio",5,263,true,60,"Interno",true,false,"Sesión individual con entrenador personal."),
            ("Piscina","Piscina",null,0,false,0,"Interno",true,false,"Acceso a piscina."),
            ("Alimentación","Alimentación",null,0,false,0,"Interno",false,false,"Servicios de alimentos."),
            ("Desayuno continental","Alimentación",9,55,false,0,"Interno",true,false,"Desayuno continental con café, sándwich y jugo."),
            ("Almuerzo","Alimentación",9,155,false,0,"Interno",true,false,"Servicio de almuerzo."),
            ("Servicio de habitación","Alimentación",9,167,false,0,"Interno",true,false,"Servicio de alimentos en habitación."),
            ("Transporte","Transporte",null,0,false,0,"Interno",false,false,"Servicios de transporte."),
            ("Traslado al aeropuerto","Transporte",13,100,false,0,"Taxi Ejecutivo",true,true,"Traslado solicitado entre el hotel y el aeropuerto."),
            ("Taxi urbano","Transporte",13,45,false,0,"Taxi Ejecutivo",true,true,"Servicio de taxi urbano."),
            ("Recreación","Recreación",null,0,false,0,"Interno",false,false,"Actividades recreativas."),
            ("Tour guiado","Recreación",16,180,true,120,"Andes Tours",true,false,"Actividad guiada por proveedor externo."),
            ("Alquiler de bicicleta","Recreación",16,90,false,0,"Rent a Car Central",true,false,"Alquiler de bicicleta."),
            ("Bienestar","Bienestar",null,0,false,0,"Interno",false,false,"Actividades de bienestar."),
            ("Yoga","Bienestar",19,75,true,45,"Interno",true,false,"Sesión de yoga."),
            ("Meditación","Bienestar",19,60,false,0,"Interno",true,false,"Sesión de meditación."),
            ("Habitación","Habitación",null,0,false,0,"Interno",false,false,"Servicios asociados a la habitación."),
            ("Lavandería","Habitación",22,80,false,0,"Interno",true,false,"Servicio de lavandería.")
        };
        for(var i=0;i<serviceSeeds.Length;i++)
        {
            var item=serviceSeeds[i];
            Servicios.Add(new ServicioModel{Id=i+1,Nombre=item.Name,Categoria=item.Category,PadreId=item.Parent,Precio=item.Price,RequiereCita=item.Appointment,DuracionMinutos=item.Duration,Proveedor=item.Provider,Reservable=item.Bookable,RequiereTraslado=item.Transfer,Descripcion=item.Description,Cobertura="No incluido",UsosRestantes=null});
        }
        Servicios.First(item=>item.Id==15).Disponible=false;
        // Package catalog relationships are copied into reservation-purchase snapshots below.
        PaquetesServicios.AddRange([
            new(1,2,"Ilimitado",null,true), new(1,3,"Por estancia",2,true), new(1,6,"Ilimitado",null,true), new(1,8,"Ilimitado",null,true), new(1,20,"Por estancia",2,true), new(1,21,"Ilimitado",null,true),
            new(2,10,"Por día",1,true),
            new(3,14,"Por estancia",1,false), new(3,17,"Por estancia",1,false)
        ]);
        RecursosAdicionales.AddRange([
            new() { RecursoId=1, Nombre="Cama adicional", Tipo="MOBILIARIO", Precio=100m, Descripcion="Cama individual adicional. No modifica la capacidad autorizada de la habitación.", Activo=true, TipoCobro="POR_NOCHE", StockTotal=15, RequierePreparacion=true },
            new() { RecursoId=2, Nombre="Almohada viscoelástica", Tipo="ROPA_DE_CAMA", Precio=20m, Descripcion="Almohada de espuma con memoria.", Activo=true, TipoCobro="POR_ESTANCIA", StockTotal=30, RequierePreparacion=true },
            new() { RecursoId=3, Nombre="Almohada hipoalergénica", Tipo="ROPA_DE_CAMA", Precio=0m, Descripcion="Almohada hipoalergénica sin costo.", Activo=true, TipoCobro="POR_ESTANCIA", StockTotal=25, RequierePreparacion=true },
            new() { RecursoId=4, Nombre="Cuna para bebé", Tipo="MOBILIARIO", Precio=0m, Descripcion="Cuna portátil para la habitación.", Activo=true, TipoCobro="POR_ESTANCIA", StockTotal=4, RequierePreparacion=true },
            new() { RecursoId=5, Nombre="Manta adicional", Tipo="ROPA_DE_CAMA", Precio=15m, Descripcion="Manta adicional para la estancia.", Activo=true, TipoCobro="POR_ESTANCIA", StockTotal=20, RequierePreparacion=true },
            new() { RecursoId=6, Nombre="Silla ergonómica", Tipo="EQUIPAMIENTO", Precio=30m, Descripcion="Silla ergonómica de apoyo para la habitación.", Activo=true, TipoCobro="POR_ESTANCIA", StockTotal=null, RequierePreparacion=true }
        ]);
        ServiciosProductos.AddRange([new(10,5,1),new(10,19,1),new(10,8,1)]);
        // Reservation #3001 is a shared-room example: two guests share one acquired limited allowance.
        var sharedPackage = new ReservaPaqueteModel(3001,1,1,720m);
        if (!ReservaPaquetes.Any(item=>item.ReservaId==sharedPackage.ReservaId&&item.PaqueteId==sharedPackage.PaqueteId)) ReservaPaquetes.Add(sharedPackage);
        FreezePackageBenefits(3001);
        foreach(var purchased in ReservaPaquetes.ToList()) FreezePackageBenefits(purchased.ReservaId);
        // Snapshot an included breakfast bundle with the purchased reservation conditions.
        foreach(var purchased in ReservaPaquetes.Where(item=>item.PaqueteId==2)) foreach(var product in ServiciosProductos.Where(item=>item.ServicioId==10))
            if(!ReservaPaqueteServicioProductos.Any(item=>item.ReservaId==purchased.ReservaId&&item.PaqueteId==purchased.PaqueteId&&item.ServicioId==10&&item.ProductoId==product.ProductoId)) ReservaPaqueteServicioProductos.Add(new(purchased.ReservaId,purchased.PaqueteId,10,product.ProductoId,product.CantidadIncluida));
        UsosServicio.AddRange([
            new UsoServicioModel(1,1,3,DateTime.Today.AddDays(-1).AddHours(10),true,0,"Completado"){EstanciaId=1},
            new UsoServicioModel(2,2,11,DateTime.Today.AddDays(-1).AddHours(8),false,168,"Completado"){EstanciaId=2},
            new UsoServicioModel(3,250,7,DateTime.Today.AddDays(-1).AddHours(14),false,107,"Completado"){EstanciaId=81},
            new UsoServicioModel(4,4,3,DateTime.Today.AddDays(-1).AddHours(10),true,0,"Completado"){EstanciaId=4},
            new UsoServicioModel(5,4,3,DateTime.Today.AddHours(9),true,0,"Completado"){EstanciaId=4}
        ]);
        // One companion has their own active-stay record while sharing the reservation, room and package.
        Estancias.Add(new EstanciaModel(81,3001,250,DateTime.Today.AddDays(-1),Reservas.First(item=>item.Id==3001).Salida,"Activa"){FolioId=1});
        SolicitudesEspeciales.AddRange([new(1,3004,"Habitación alejada del ascensor","Pendiente"),new(2,3009,"Almohada adicional","Atendida"),new(3,3014,"Llegada después de las 22:00","Pendiente")]);
        var appointmentServices=Servicios.Where(item=>item.RequiereCita&&item.Reservable).ToList();
        for(int i=1;i<=30;i++)
        {
            var guestId=i; var serviceId=appointmentServices[(i-1)%appointmentServices.Count].Id; var start=DateTime.Today.AddHours(8+i%11);
            var state=i%9==0?"Completada":i%7==0?"En curso":"Confirmada";
            var stay=Estancias.FirstOrDefault(item=>item.HuespedId==guestId&&item.Estado=="Activa");
            Citas.Add(new CitaServicioModel(i,guestId,serviceId,start,start.AddMinutes(SampleData.Servicios.First(item=>item.Id==serviceId).DuracionMinutos),Empleados.FirstOrDefault(item=>item.Cargo=="Servicios")?.Nombre??"Por asignar",state){EstanciaId=stay?.Id,Confirmada=state=="Confirmada"?DateTime.Today.AddHours(7):null,UsuarioConfirmaId=3});
        }
        var serviceStaff=Empleados.FirstOrDefault(item=>item.Cargo=="Servicios");
        SolicitudesServicio.AddRange([
            new SolicitudServicioModel(1,4,3,DateTime.Now.AddHours(-2),DateTime.Today.AddDays(1).AddHours(15),"Masaje relajante después del almuerzo.","Solicitada",true){EstanciaId=4,ReservaId=3004,UsuarioSolicitaId=2,CitaServicioId=31,CoberturaEstimada="Incluido · pendiente de confirmar"},
            new SolicitudServicioModel(2,9,11,DateTime.Now.AddHours(-1),null,"Sin observaciones.","Pendiente",false){EstanciaId=9,ReservaId=3009,UsuarioSolicitaId=2,CoberturaEstimada="Servicio adicional · Bs 155.00"},
            new SolicitudServicioModel(3,14,7,DateTime.Now.AddHours(-4),DateTime.Today.AddDays(1).AddHours(10),"Entrenador personal.","Confirmada",true){EstanciaId=14,ReservaId=3014,UsuarioSolicitaId=2,CitaServicioId=32,ResponsableId=serviceStaff?.Id,Responsable=serviceStaff?.Nombre??"",Aceptada=DateTime.Now.AddHours(-3),CoberturaEstimada="Servicio adicional · Bs 263.00"}
        ]);
        Citas.Add(new CitaServicioModel(31,4,3,DateTime.Today.AddDays(1).AddHours(15),DateTime.Today.AddDays(1).AddHours(15).AddMinutes(45),"Por asignar","Solicitada"){EstanciaId=4,SolicitudId=1});
        Citas.Add(new CitaServicioModel(32,14,7,DateTime.Today.AddDays(1).AddHours(10),DateTime.Today.AddDays(1).AddHours(11),serviceStaff?.Nombre??"Por asignar","Confirmada"){EstanciaId=14,SolicitudId=3,UsuarioConfirmaId=3,Confirmada=DateTime.Now.AddHours(-3)});
        string[] pn=["Agua mineral","Gaseosa","Chocolate artesanal","Nueces mixtas","Café molido","Té de hierbas","Vino de la casa","Jugo natural","Barra energética","Galletas integrales","Papas crocantes","Jabón premium","Kit dental","Bata de baño","Toalla facial","Cerveza local","Maní tostado","Fruta de estación","Sándwich club","Ensalada fresca"];
        var categories=new[]{"Minibar","Restaurante","Habitación","Bienestar"};
        for(int i=0;i<40;i++) Productos.Add(new(){Id=i+1,Nombre=pn[i%pn.Length]+(i>=20?$" {i/20+1}":""),Categoria=categories[i%4],Precio=12m+(i%8)*7m,Stock=i%9==0?3:18+i%20,StockMinimo=8,IncluidoEnPaquete=i%10==0});
        for(int i=1;i<=35;i++)
        {
            var room=Habitaciones[82+i];
            var taskState=i==2?"En progreso":i==3?"Completada":i==4?"Inspeccionada":"Pendiente";
            var cleaning=taskState switch {"En progreso"=>"En limpieza","Completada"=>"Pendiente de inspección","Inspeccionada"=>"Limpia",_=>"Pendiente de limpieza"};
            room.Limpieza=cleaning;
            var task=new TareaModel{Id=i,Habitacion=room.Id,Piso=room.Piso,Tipo=i%3==0?"Salida":"Repaso",Estado=taskState,Empleado=taskState=="Pendiente"?"":$"Equipo {i%6+1}",Prioridad=i==1||i==2?"Urgente":"Normal",Hora=DateTime.Today.AddHours(8+i%10),Observaciones=i==1?"Preparación para llegada de hoy.":""};
            Tareas.Add(task);
            if(taskState=="Inspeccionada")Inspecciones.Add(new(i,task.Id,DateTime.Today.AddHours(15),true,"Inspección aprobada."));
            if(taskState=="Completada")Inspecciones.Add(new(i,task.Id,DateTime.Today.AddHours(15),false,"Pendiente de inspección."));
        }
        string[] incidents=["Aire acondicionado no enfría","Lámpara con falla","Fuga bajo el lavabo","Control remoto sin respuesta","Bisagra de puerta floja","Televisor sin señal","Grifo con goteo","Enchufe por revisar","Cortina fuera de riel","Cerradura requiere ajuste","Ducha con baja presión","Ventana no cierra bien"];
        for(int i=1;i<=12;i++) Incidencias.Add(new(){Id=i,Habitacion=Habitaciones[(i*9)%120].Id,Descripcion=incidents[i-1],Prioridad=i%5==0?"Alta":"Media",Estado=i%4==0?"En reparación":"Abierta",BloqueaHabitacion=i%6==0,Categoria=i%3==0?"Plomería":"Equipamiento",FechaReporte=DateTime.Today.AddDays(-i),ReportadaPor="Equipo de mantenimiento"});
        var occupiedBlocked=Habitaciones[36];occupiedBlocked.Bloqueada=true;
        Incidencias.Add(new(){Id=Incidencias.Max(i=>i.Id)+1,Habitacion=occupiedBlocked.Id,Descripcion="Fuga de agua; se requiere reubicar al huésped",Categoria="Plomería",Prioridad="Urgente",Estado="En reparación",BloqueaHabitacion=true,FechaReporte=DateTime.Now.AddHours(-1),ReportadaPor="Encargado de servicios"});
        Habitaciones[88].Bloqueada=true;
        Incidencias.Add(new(){Id=Incidencias.Max(i=>i.Id)+1,Habitacion=Habitaciones[88].Id,Descripcion="Reparación preventiva de mobiliario",Categoria="Mobiliario",Prioridad="Alta",Estado="En reparación",BloqueaHabitacion=true,FechaReporte=DateTime.Today.AddHours(-2),ReportadaPor="Encargado de servicios"});
        Incidencias.Add(new(){Id=Incidencias.Max(i=>i.Id)+1,Habitacion=Habitaciones[87].Id,Descripcion="Televisor no enciende; revisión solicitada",Categoria="Equipamiento",Prioridad="Media",Estado="Abierta",BloqueaHabitacion=false,FechaReporte=DateTime.Today.AddHours(-1),ReportadaPor="Recepción"});
        var turnoverRoom=Habitaciones[4];
        var turnoverGuest=Huespedes[180];
        var turnoverReservation=new ReservaModel{Id=5000,Huesped=turnoverGuest.Nombre,Habitacion=turnoverRoom.Id,Llegada=DateTime.Today,Salida=DateTime.Today.AddDays(2),Estado="Confirmada",Canal="Directa",Personas=1};
        Reservas.Add(turnoverReservation);ReservaHabitaciones.Add(new(5000,turnoverReservation.Id,turnoverRoom.Id));ReservaHuespedes.Add(new(turnoverReservation.Id,turnoverGuest.Id,true));ReservaHabitacionHuespedes.Add(new(5000,turnoverReservation.Id,turnoverGuest.Id));
        Inspecciones.Add(new(100, Tareas.First(t=>t.Habitacion==Habitaciones[86].Id).Id, DateTime.Today.AddHours(-2), false, "No aprobada: revisar limpieza del baño."));
        ObjetosOlvidados.AddRange([new(1,"Auriculares inalámbricos",310,DateTime.Today,"En custodia","Encontrados en la mesa de noche"),new(2,"Bufanda azul",221,DateTime.Today.AddDays(-1),"En custodia","Entregada a recepción"),new(3,"Libro de viaje",407,DateTime.Today.AddDays(-7),"Devuelto","Retirado por el huésped")]);
        for(int i=1;i<=8;i++) Cargos.Add(new(i,i%2==0?"Consumo de minibar":"Servicio de habitación",240+i*40,DateTime.Today.AddDays(-i),i));
        for(int i=1;i<=24;i++) Consumos.Add(new(i,(i%8)+1,(i%Productos.Count)+1,1+i%2,DateTime.Today.AddDays(-i%5),false));
        Movimientos.AddRange([new(DateTime.Today.AddHours(8),"Apertura","Fondo inicial",AperturaCaja,"Efectivo"),new(DateTime.Today.AddHours(9),"Pago","Reserva #3054",180,"Efectivo",1),new(DateTime.Today.AddHours(10),"Pago","Folio habitación 204",150,"Tarjeta",2),new(DateTime.Today.AddHours(11),"Pago","Reserva #3098",120,"Transferencia",3)]);
        for(int i=1;i<=60;i++)Facturas.Add(new(i,3000+i,$"F-2026-{i:000}",DateTime.Today.AddDays(-i%30),240m+i*35m,i%9==0?"Pendiente":"Emitida"));

        // Explicit folios for confirmed reservations support advances before check-in.
        foreach (var reservation in Reservas.Where(item => item.Estado == "Confirmada"))
        {
            if (Folios.Any(item => item.ReservaId == reservation.Id)) continue;
            var primaryId = ReservaHuespedes.FirstOrDefault(item => item.ReservaId == reservation.Id && item.Principal)?.HuespedId;
            if (primaryId is null) continue;
            var folioId = Folios.Count == 0 ? 1 : Folios.Max(item => item.Id) + 1;
            Folios.Add(new FolioModel(folioId, null, 0, "Abierto") { ReservaId = reservation.Id, HuespedPrincipalId = primaryId, Tipo = "Reserva" });
            var nights = Math.Max(1, (reservation.Salida.Date - reservation.Llegada.Date).Days);
            var amount = Tarifas.GetValueOrDefault(reservation.Habitacion, 420m) * nights;
            Cargos.Add(new CargoModel(Cargos.Count + 1, $"Alojamiento · Reserva #{reservation.Id}", amount, reservation.Llegada, primaryId.Value) { FolioId = folioId });
        }

        // The cashier prototype has independent shifts, including prior shifts and a second receptionist.
        var shiftToday = DateTime.Today;
        TurnosCaja.Add(new(1, 2, shiftToday.AddHours(8), 1200m, "Abierto"));
        var yesterday = shiftToday.AddDays(-1);
        TurnosCaja.Add(new(2, 2, yesterday.AddHours(8), 800m, "Cerrado") { Cierre = yesterday.AddHours(16), EfectivoContado = 1120m, CierreCajaId = 1, ObservacionesCierre = "Diferencia revisada en el arqueo." });
        TurnosCaja.Add(new(3, 2, shiftToday, 300m, "Cerrado") { Cierre = shiftToday.AddHours(6), EfectivoContado = 500m, CierreCajaId = 2 });
        TurnosCaja.Add(new(4, 5, yesterday.AddHours(22), 500m, "Cerrado") { Cierre = shiftToday.AddHours(6), EfectivoContado = 700m, CierreCajaId = 3, ObservacionesCierre = "Turno nocturno." });
        TurnosCaja.Add(new(5, 5, shiftToday.AddHours(8), 650m, "Abierto"));
        CajaAbierta = true;
        CierresCaja.Add(new CierreCajaModel(1, yesterday.AddHours(16), 1380m, 1330m, -50m, "Diferencia revisada en el arqueo.") { TurnoCajaId = 2, UsuarioId = 2, Apertura = yesterday.AddHours(8), AperturaMonto = 800m, EfectivoCobros = 580m, EfectivoDevoluciones = 0m, NoEfectivo = 300m });
        CierresCaja.Add(new CierreCajaModel(2, shiftToday.AddHours(6), 500m, 500m, 0m, "") { TurnoCajaId = 3, UsuarioId = 2, Apertura = shiftToday, AperturaMonto = 300m, EfectivoCobros = 200m, EfectivoDevoluciones = 0m });
        CierresCaja.Add(new CierreCajaModel(3, shiftToday.AddHours(6), 680m, 700m, 20m, "Turno nocturno; sobrante documentado.") { TurnoCajaId = 4, UsuarioId = 5, Apertura = yesterday.AddHours(22), AperturaMonto = 500m, EfectivoCobros = 200m, EfectivoDevoluciones = 20m });

        Cargos.Add(new CargoModel(Cargos.Count + 1, "Alojamiento · habitación 101", 500m, shiftToday.AddHours(7), 1, 1) { FolioId = 1 });
        Cargos.Add(new CargoModel(Cargos.Count + 1, "Alojamiento · habitación 102", 420m, shiftToday.AddHours(7), 2, 2) { FolioId = 2 });
        Cargos.Add(new CargoModel(Cargos.Count + 1, "Alojamiento · habitación 103", 780m, shiftToday.AddHours(7), 3, 3) { FolioId = 3 });
        Cargos.Add(new CargoModel(Cargos.Count + 1, "Estancia histórica · huésped 1", 800m, yesterday.AddDays(-3), 1) { FolioId = 1000 });
        Cargos.Add(new CargoModel(Cargos.Count + 1, "Estancia histórica · huésped 2", 600m, yesterday.AddDays(-2), 2) { FolioId = 1001 });
        Cargos.Add(new CargoModel(Cargos.Count + 1, "Estancia histórica · huésped 3", 500m, yesterday.AddDays(-1), 3) { FolioId = 1002 });
        SeedPayment(1, 1, 180m, "Efectivo", shiftToday.AddHours(9), 1, "Pago de folio");
        SeedPayment(2, 2, 150m, "Tarjeta", shiftToday.AddHours(10), 1, "Pago de folio");
        SeedPayment(3, 3, 120m, "Transferencia", shiftToday.AddHours(11), 1, "Pago de folio");
        SeedPayment(4, 1000, 500m, "Efectivo", yesterday.AddHours(9), 2, "Pago histórico");
        SeedPayment(5, 1000, 80m, "Efectivo", yesterday.AddHours(12), 2, "Pago histórico");
        SeedPayment(6, 1001, 300m, "Tarjeta", yesterday.AddHours(13), 2, "Pago histórico");
        SeedPayment(7, 1002, 200m, "Efectivo", yesterday.AddHours(23), 4, "Pago de turno nocturno");
        SeedPayment(8, 1, 125m, "Tarjeta", shiftToday.AddHours(9), 5, "Pago de otro recepcionista");
        SeedPayment(9, 1001, 200m, "Efectivo", shiftToday.AddHours(5), 3, "Pago de apertura temprana");
        SeedRefund(1, 4, 80m, "Cobro duplicado", "Pendiente");
        SeedRefund(2, 5, 30m, "Importe incorrecto", "Autorizada");
        SeedRefund(3, 7, 20m, "Cobro duplicado", "Autorizada", 4);
        Anulaciones.Add(new(1, 6, "Método de pago registrado incorrectamente", "Pendiente") { Solicitada = shiftToday.AddHours(11), UsuarioSolicitaId = 2, Descripcion = "Revisar el comprobante con el huésped." });
        Facturas.Add(new FacturaModel(100, 1000, "FAC-2026-100", yesterday.AddDays(-3), 800m, "Emitida")
        {
            Subtotal = 800m, Cliente = Huespedes[0].Nombre, DocumentoCliente = Huespedes[0].Documento, UsuarioEmiteId = 2,
            Lineas = [new(0, "Estancia histórica · huésped 1", 1, 800m, 800m)], EstanciaId = 1000
        });
    }

    private static void FreezePackageBenefits(int reservationId)
    {
        foreach (var purchase in ReservaPaquetes.Where(item => item.ReservaId == reservationId))
        foreach (var benefit in PaquetesServicios.Where(item => item.PaqueteId == purchase.PaqueteId))
            if (!ReservaPaqueteServicios.Any(item => item.ReservaId == reservationId && item.PaqueteId == purchase.PaqueteId && item.ServicioId == benefit.ServicioId))
                ReservaPaqueteServicios.Add(new(reservationId, purchase.PaqueteId, benefit.ServicioId, benefit.TipoCobertura, benefit.LimiteUsos, benefit.Compartido));
    }

    private static void SeedPayment(int id, int folioId, decimal amount, string method, DateTime at, int shiftId, string concept)
    {
        var userId = TurnosCaja.First(item => item.Id == shiftId).UsuarioId;
        Pagos.Add(new PagoModel(id, folioId, amount, method, at, "Aplicado") { TurnoCajaId = shiftId, UsuarioId = userId, Concepto = concept });
        MovimientosCaja.Add(new MovimientoCajaModel(MovimientosCaja.Count + 1, shiftId, at, "Cobro", amount, concept)
        { PagoId = id, FolioId = folioId, Metodo = method, UsuarioId = userId });
    }

    private static void SeedRefund(int id, int paymentId, decimal amount, string reason, string state, int? executionShiftId = null)
    {
        var payment = Pagos.First(item => item.Id == paymentId);
        var userId = TurnosCaja.First(item => item.Id == payment.TurnoCajaId).UsuarioId;
        var refund = new DevolucionModel(id, paymentId, amount, reason, state)
        { Solicitada = payment.Fecha.AddHours(1), UsuarioSolicitaId = userId, UsuarioAutorizaId = state == "Autorizada" ? 4 : null, FechaAutorizacion = state == "Autorizada" ? payment.Fecha.AddHours(2) : null };
        Devoluciones.Add(refund);
        if (executionShiftId is int shiftId)
        {
            Devoluciones[^1] = Devoluciones[^1] with { Estado = "Ejecutada", Ejecutada = TurnosCaja.First(item => item.Id == shiftId).Apertura.AddHours(2), TurnoEjecucionId = shiftId, MetodoEjecucion = payment.Metodo };
            MovimientosCaja.Add(new(MovimientosCaja.Count + 1, shiftId, Devoluciones[^1].Ejecutada!.Value, "Devolución", amount, reason)
            { PagoId = paymentId, FolioId = payment.FolioId, Metodo = payment.Metodo, UsuarioId = TurnosCaja.First(item => item.Id == shiftId).UsuarioId, CorreccionId = id });
        }
    }
}
