using Microsoft.Extensions.DependencyInjection;

using NuminaBit.Console.Services.Shared;
using NuminaBit.Console.Services.Shared.Intefaces;

using NuminaBit.Services;

var services = new ServiceCollection();

services.AddNuminaBitServices();
services.AddTransient<IAttackRunnerExamples, AttackRunnerExamples>();
services.AddTransient<IEquationBuilderExamples, EquationBuilderExamples>(); 

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();

var equationBuilderExamples = scope.ServiceProvider.GetRequiredService<IEquationBuilderExamples>();
var attackRunner2Examples = scope.ServiceProvider.GetRequiredService<IAttackRunnerExamples>();

attackRunner2Examples.Example2();

Console.ReadLine();