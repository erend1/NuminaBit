using Microsoft.Extensions.DependencyInjection;

using NuminaBit.Console.Services.Shared;
using NuminaBit.Console.Services.Shared.Intefaces;

using NuminaBit.Services;

var services = new ServiceCollection();

services.AddNuminaBitServices();
services.AddTransient<IAttackRunner2Examples, AttackRunner2Examples>();
services.AddTransient<IEquationBuilderExamples, EquationBuilderExamples>(); 

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();

var myService = scope.ServiceProvider.GetRequiredService<IAttackRunner2Examples>();

myService.Example1();

Console.ReadLine();