using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwagMatch.CLI;
using SwagMatch.Core.Macher;

IHost host = Configure.AppHost(args);
SwaggerMach swagger = host.Services.GetRequiredService<SwaggerMach>();
(string path, int bytesWritten) = await swagger.CompareAsync();