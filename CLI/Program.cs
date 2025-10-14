using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CLI;

IHost host = Configure.AppHost(args);
Core.SwagMatch swagger = host.Services.GetRequiredService<Core.SwagMatch>();
(string path, int bytesWritten) = await swagger.CompareAsync();