using Microsoft.Extensions.DependencyInjection;

using NuminaBit.Console.Services.Shared;
using NuminaBit.Console.Services.Shared.Intefaces;

using NuminaBit.Services;

var services = new ServiceCollection();

services.AddNuminaBitServices(); // Your DLL extension method
services.AddTransient<IEquationBuilderExamples, EquationBuilderExamples>(); // Register your service

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();

var myService = scope.ServiceProvider.GetRequiredService<IEquationBuilderExamples>();

myService.Example2();

Console.ReadLine();