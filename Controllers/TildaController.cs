using Altinn.ApiClients.Dan.Interfaces;
using Dan.Tilda.Models.Audits;
using Dan.Tilda.Models.Audits.Coordination;
using Dan.Tilda.Models.Audits.Report;
using Dan.Tilda.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using tilda_reference_client.Models;

namespace tilda_reference_client.Controllers;

[ApiController]
[Route("api/v1/tilda")]
public class TildaController(IDanClient danClient) : Controller
{
    [HttpGet("rapport/{subject}")]
    public async Task<ActionResult> GetReportList(string subject)
    {
        var datasetname = "TildaTilsynsrapportv1";
        var audits = await GetAudits<AuditReportList>(datasetname, subject, "tilsynsrapporter");
        return Ok(audits);
    }
    
    [HttpGet("rapport/{subject}/alle")]
    public async Task<ActionResult> GetReportListAlle(string subject)
    {
        var datasetname = "TildaTilsynsrapportAllev1";
        
        var parameters = new Dictionary<string, string>
        {
            { "aar", "2024" },
            { "maaned", "1" }
        };
        var dataset2 = await danClient.GetDataSet<AuditReportList>(datasetname, subject, parameters:parameters, deserializeField: "tilsynsrapporter");
        return Ok(dataset2);
    }
    
    [HttpGet("koordinering/{subject}")]
    public async Task<ActionResult> GetCoordinationList(string subject)
    {
        var datasetname = "TildaTilsynskoordineringv1";
        var audits = await GetAudits<AuditCoordinationList>(datasetname, subject, "tilsynskoordineringer");
        return Ok(audits);
    }
    
    [HttpGet("koordinering/{subject}/alle")]
    public async Task<ActionResult> GetCoordinationListAlle(string subject)
    {
        var datasetname = "TildaTilsynskoordineringAllev1";
        
        // Gets a generic dataset which can be iterated
        var parameters = new Dictionary<string, string>
        {
            { "aar", "2023" },
            { "maaned", "2" }
        };
        var dataset2 = await danClient.GetDataSet<AuditCoordinationList>(datasetname, subject, parameters:parameters, deserializeField: "tilsynskoordineringer");
        return Ok(dataset2);
    }

    private async Task<Audits<T>> GetAudits<T>(string datasetname, string subject, string deserializeField) where T : IAuditList
    {
        var dataset = await danClient.GetDataSet(datasetname, subject);
        var list = new List<T?>();
        var entry = new TildaRegistryEntry();
        foreach (var dsv in dataset.Values)
        {
            var dsvValue = dsv.Value.ToString();
            if (dsvValue is null)
            {
                // Handle error etc.
                continue;
            }
            if (dsv.Name == "enhetsinformasjon")
            {
                entry = JsonConvert.DeserializeObject<TildaRegistryEntry>(dsvValue);
            }

            if (dsv.Name == deserializeField)
            {
                var y = JsonConvert.DeserializeObject<T>(dsvValue);
                list.Add(y);
            }
        }

        var audits = new Audits<T>
        {
            Entry = entry,
            AuditList = list
        };

        return audits;
    }
}