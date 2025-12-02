using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Configure.AppHost(args);
SwagMatch.Core.Domain.SwagMatch swagger = host.Services.GetRequiredService<SwagMatch.Core.Domain.SwagMatch>();

(int bytesWritten, string fileName) = await swagger.CompareSwaggers();