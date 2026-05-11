# tilda-reference-client
Reference implementation of a Tilda client.

This uses three Nuget packages for fetching Tilda data:
- Altinn.ApiClients.Maskinporten
- Altinn.ApiClients.Dan
- Dan.Tilda.Models

Altinn.ApiClients.Maskinporten is used to get a Maskinporten-token used for 
authentication and authorisation against data.altinn.no (DAN)

Altinn.ApiClients.Dan is used to facilitate REST-calls to DAN.

Dan.Tilda.Models is used to map the responses from DAN to Tilda-models using the
same classes DAN uses.

## Setting up Maskinporten-client and DAN-client

In `Program.cs` the clients used are set up for dependency injection.
```csharp
builder.Services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>("my-client-definition-for-dan", 
    builder.Configuration.GetSection("MaskinportenSettings"));

builder.Services
    .AddDanClient(builder.Configuration.GetSection("DanSettings"), conf => new DanConfiguration
    {
        Deserializer = new JsonNetDeserializer()
    })
    .AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>("my-client-definition-for-dan");

builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json");
```

The first section registers a MaskinportenClient that can be added as a `HttpMessageHandler` for the
DAN-client. `"my-client-definition-for-dan"` is the name for the client, and `"MaskinportenSettings"` 
points to the application settings. See https://github.com/Altinn/altinn-apiclient-maskinporten for
further documentation and source code.

The second section registers a DanClient. It first uses the `"DanSettings"` section of the
application settings, then we set up the deserializer the client uses for a Newtonsoft.Json
serializer, as the Dan.Tilda.Models package uses Newtonsoft.Json and not System.Text.Json. Can
be omitted if not using Dan.Tilda.Models for models to deserialise into. Finally we register
the HttpMessageHandler that was registered in the previous step. This will make the DanClient set
the correct authentication header. See https://github.com/data-altinn-no/altinn-apiclient-dan/ for
further documentation and source code.

Final step is setting up the application settings. In this example we just register them from a
local `appsettings.{environment}.json` file, but any way to set up application settings is valid.

## Example application settings
As mentioned in the previous settings, the MaskinportenClient and DanClient looks for their respective
application settings, which we have named `"MaskinportenSettings"` and `"DanSettings"`.

```jsonc
"DanSettings": {
    "Environment": "staging",
    "SubscriptionKey": "" // dan subscription key
  },
  "MaskinportenSettings": {
    "Environment": "test",
    "ClientId": "", // maskinporten client id
    "Scope": "altinn:dataaltinnno/tilda",
    "EncodedJwk": "" // maskinporten jwk encoded to base64
  }
```

For DanSettings:
 - `Environment` valid values are `local`, `dev`, `staging` or `prod`
 - `SubscriptionKey` is your APIM subscription key for DAN

For MaskinportenSettings:
- `Environment` valid values are `test` or `prod`. `prod` is only when DanSettings Environment
is also set to `prod`
- `ClientId` is your Maskinporten client ID
- `Scope` should be set to `altinn:dataaltinnno/tilda`
- `EncodedJwk` is a Base64 Encoded value of your JWK used to get auth tokens from Maskinporten

## Using DanClient
When the DanClient has been registered, it can be used in classes via dependency injection using
the `IDanClient` interface.

```csharp
public class ConstructorExample(IDanClient danClient)
```

Then the method `GetDataSet()` can be called to fetch data. This method requires a dataset name
and a subject as a base requirement. The equivalent of calling
`https://api.data.altinn.no/v1/directharvest/TildaTilsynskoordineringv1?subject=112233445` would be
```cs
var result = await GetDataSet("TildaTilsynskoordineringv1", "112233445");
```

The result of the method is a set of Dataset values. These can be iterated over in a loop:

```csharp
foreach (var dsv in dataset.Values)
{
    // Do something with dsv.Name and dsv.Value
}
```

For the "non-Alle" datasets, such as `TildaTilsynskoordineringv1` it will come with two named values.
One is `enhetsinformasjon`, and for `TildaTilsynskoordineringv1` it's `tilsynskoordineringer`. We
can use these names to deserialize into the models from Dan.Tilda.Models.

```csharp
foreach (var dsv in dataset.Values)
{
    var dsvValue = (string)dsv.Value;
    if (dsv.Name == "enhetsinformasjon")
    {
        var entry = JsonConvert.DeserializeObject<TildaRegistryEntry>(dsvValue);
        // do something with entry
    }

    if (dsv.Name == "tilsynskoordineringer")
    {
        var audit = JsonConvert.DeserializeObject<AuditCoordinationList>(dsvValue);
        // do something with audit
    }
}
```

For the Alle-datasets, such as TildaTilsynskoordineringAllev1, we are able to immediately deserialise
into the Dan.Tilda.Models-class since there is only one dataset value to work with. The Alle-datasets
also require some extra parameters, normally passed as query parameters, and we specify the dataset field
that we will deserialise from, in this case `"tilsynskoordineringer"`.

```csharp
var parameters = new Dictionary<string, string>
{
    { "aar", "2023" },
    { "maaned", "2" }
};
var dataset = await danClient.GetDataSet<AuditCoordinationList>("TildaTilsynskoordineringAllev1", subject: "112233445", parameters:parameters, deserializeField: "tilsynskoordineringer");
```

See TildaController.cs for a comprehensive example of fetching different datasets and a generic method
for ease of use for fetching different dataset types.