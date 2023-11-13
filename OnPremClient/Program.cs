using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;


var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

const string CERT_SUBJECT = "selfsigned-demo";

string clientId = config["ClientId"]; //ClientId of the app registration
string tenantId = config["TenantId"]; //Tenant of the EntraId Tenant
string apiScope = config["ApiScope"]; //Scope of the API -> "api://<clientid of the api>/.default"


X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

X509Certificate2 cert = null;
try
{
    store.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection temp = store.Certificates;
    X509Certificate2Collection certificateCollection = store.Certificates.Find(X509FindType.FindBySubjectName, CERT_SUBJECT, false);
    if (certificateCollection == null || certificateCollection.Count == 0)
    {
        throw new Exception("Certificate not installed in the store");
    }
    cert = certificateCollection[0];
}
finally
{
    store.Close();
}

ConfidentialClientApplicationOptions options = new();
options.ClientId = clientId;
options.TenantId = tenantId;
options.Instance = "https://login.microsoftonline.com/";


IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(options)
    .WithCertificate(cert)
    .Build();

var result = await app.AcquireTokenForClient(
            new[] { "api://4cea62fb-22a4-4391-b63d-987508eaf05a/.default" })
            .ExecuteAsync();

Console.WriteLine(result.AccessToken);