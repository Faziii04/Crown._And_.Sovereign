using Crown___Sovereign.Models;

namespace Crown___Sovereign.Models;

public sealed record ServiceCategoryGroup(string Name, IReadOnlyList<ServiceParentGroup> Groups);
public sealed record ServiceParentGroup(string Name, IReadOnlyList<ServicioModel> Services);
public sealed record ServiceBenefitRow(string Service, string Category, string Coverage, string Remaining, string Price, bool RequiresAppointment);
public sealed record ServiceUsageHistoryRow(DateTime Fecha, string Servicio, string Cobertura, decimal Cargo, string Estado);
public sealed record ServiceUsageCard(ServicioModel Service, string Coverage, string Price, string Action, string Detail = "", bool Available = true);
public sealed record AppointmentQueueRow(int Id, DateTime Start, DateTime End, string Service, string Guest, string Room, string Employee, string Status, string Category);
public sealed class ServiceTreeNode
{
    public ServicioModel Service { get; init; } = null!;
    public string Name => Service.Nombre;
    public IReadOnlyList<ServiceTreeNode> Children { get; init; } = [];
}
