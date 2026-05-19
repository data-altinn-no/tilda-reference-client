using Dan.Tilda.Models.Audits;
using Dan.Tilda.Models.Entities;

namespace tilda_reference_client.Models;

public class Audits<T> where T : IAuditList
{
    public TildaRegistryEntry? Entry { get; set; }
    
    public List<T?>? AuditList { get; set; }
}