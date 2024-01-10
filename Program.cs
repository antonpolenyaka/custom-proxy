//var builder = WebApplication.CreateBuilder(args);
//var app = builder.Build();
//app.MapGet("/", () => "Hello World!");
//app.Run();

// dotnet dev-certs https --trust

using Proxy;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var app = builder.Build();

    app.MapGet("/", () => "Hello World!");

    Dictionary<string, string> prefixToTarget = new Dictionary<string, string>();
    // VMPERSEOTEST09 (192.168.23.70)
    // VMPERSEOTEST10 (192.168.23.81)
    // DESKTOP-4G1AFGQ (192.168.23.148)
    prefixToTarget.Add("http://VMPERSEOTEST10:8080/", "http://DESKTOP-4G1AFGQ:80/");
    prefixToTarget.Add("http://192.168.23.81:8080/", "http://192.168.23.148:80/");
    prefixToTarget.Add("http://localhost:8080/", "http://DESKTOP-4G1AFGQ:80/");
    prefixToTarget.Add("http://localhost:80/", "http://DESKTOP-4G1AFGQ:80/");
    ProxyServer proxy = new ProxyServer(prefixToTarget);

    //await proxy.Start();
    proxy.Start();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}