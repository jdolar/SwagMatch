using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CLI;

IHost host = Configure.AppHost(args);
SwagMatch.Core.Domain.SwagMatch swagger = host.Services.GetRequiredService<SwagMatch.Core.Domain.SwagMatch>();

var info = await swagger.FindInfo();

(string path, int bytesWritten) = await swagger.CompareAsync();