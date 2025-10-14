using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Core.Client;
using SwagMatch.Core.Models.UserInput;
namespace CLI;
public sealed class Configure
{
    public static IHost AppHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration((context, config) =>
                   {
                       config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                   })
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddConsole();
                       logging.AddSimpleConsole(options =>
                       {
                           options.SingleLine = true;
                           options.TimestampFormat = "[HH:mm:ss] ";
                       });

                   })

                   .ConfigureServices((context,services) =>
                   {
                       // Config registration
                       AppSettings compareConfig = context.Configuration.Get<AppSettings>() ?? new();
                       services.AddSingleton(compareConfig);

                       // Ensure the path is set
                       if (compareConfig.Path is null || compareConfig.Path.Trim().Length == 0)
                       {
                           compareConfig.Path = Environment.CurrentDirectory;
                       }

                       //Http Client registration
                       services.AddHttpClient<IRestClient, RestClient>("InternalApi", client =>
                       {
                           client.Timeout = TimeSpan.FromSeconds(compareConfig.ApiTimeout);
                       });
            
                       // Add Compare service
                       services.AddSingleton<Core.SwagMatch>();
                   })
            
                   .Build();
    }
}
