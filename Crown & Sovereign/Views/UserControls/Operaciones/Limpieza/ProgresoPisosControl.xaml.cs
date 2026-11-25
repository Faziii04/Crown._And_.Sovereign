using System.Windows.Controls;
using Crown___Sovereign.Data;
namespace Crown___Sovereign.Views.UserControls.Operaciones.Limpieza;
public partial class ProgresoPisosControl : UserControl
{public ProgresoPisosControl(){InitializeComponent();FloorList.ItemsSource=Enumerable.Range(1,5).Select(f=>{var tasks=SampleData.Tareas.Where(t=>t.Piso==f).ToList();var done=tasks.Count(t=>t.Estado=="Inspeccionada");return new{Piso=f,Progreso=tasks.Count==0?0:done*100.0/tasks.Count,Texto=$"{done} de {tasks.Count} listas"};}).ToList();}}
