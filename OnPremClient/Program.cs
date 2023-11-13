using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;


var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

const string CERT_SUBJECT = "selfsigned-demo";

string clientId = config["ClientId"]; //ClientId of the app registration
string tenantId = config["TenantId"]; //Tenant of the EntraId Tenant
string apiScope = config["ApiScope"]; //Scope of the API -> "api://<clientid of the api>/.default"
string dbScope = "https://database.windows.net//.default";
string sqlEndpoint = config["SqlEndpoint"]; //SQL Endpoint
string sqlDatabase = config["SqlDatabase"]; //SQL Database



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

var apiTokenResult = await app.AcquireTokenForClient(
            new[] { apiScope })
            .ExecuteAsync();


Console.WriteLine();
Console.WriteLine("Acquired this JWT for API");
Console.WriteLine("----------------------------");
Console.WriteLine(apiTokenResult.AccessToken);


HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiTokenResult.AccessToken);
var response = await client.GetAsync("https://localhost:7074/WeatherForecast");

//Give the API some time to start up
Thread.Sleep(3000);

Console.WriteLine("Response from API");
Console.WriteLine("----------------------------");
Console.WriteLine($"STATUS: {response.StatusCode}");
Console.WriteLine(await response.Content.ReadAsStringAsync());



//Connect to SQL Database on Azure with token
//ServicePrincipal needs permissions on the database
var dbTokenResult = await app.AcquireTokenForClient(
            new[] { dbScope })
            .ExecuteAsync();

Console.WriteLine();
Console.WriteLine("Acquired this JWT for SQL");
Console.WriteLine("----------------------------");
Console.WriteLine(dbTokenResult.AccessToken);


// Define the retry logic parameters - when using SQL Serverless and database is asleep
var sqlRetryLogicOption = new SqlRetryLogicOption()
{
    // Tries 5 times before throwing an exception
    NumberOfTries = 5,
    // Preferred gap time to delay before retry
    DeltaTime = TimeSpan.FromSeconds(5),
    // Maximum gap time for each delay time before retry
    MaxTimeInterval = TimeSpan.FromSeconds(20)
};

// Create a retry logic provider
SqlRetryLogicBaseProvider provider = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(sqlRetryLogicOption);

SqlConnection conn = new SqlConnection($"Data Source={sqlEndpoint}; Initial Catalog={sqlDatabase}");
conn.RetryLogicProvider = provider;
conn.AccessToken = dbTokenResult.AccessToken;


Console.WriteLine();
Console.WriteLine("Response from SQL");
Console.WriteLine("----------------------------");
try
{
    conn.Open();
    Console.WriteLine("Connected to SQL Database");
    conn.Close();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}


Console.WriteLine();
Console.WriteLine("Hit any key to close...");
Console.ReadKey();
