using Microsoft.Extensions.DependencyInjection;

using NuminaBit.Services;

using NuminaBit.Console.Services.Shared;
using NuminaBit.Console.Services.Shared.Intefaces;


var services = new ServiceCollection();

services.AddNuminaBitServices();
services.AddTransient<IFirstAlgorithmExamples, FirstAlgorithmExamples>();
services.AddTransient<ISecondAlgorithmExamples, SecondAlgorithmExamples>();
services.AddTransient<IEquationBuilderExamples, EquationBuilderExamples>(); 

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();

var equationBuilderExamples = scope.ServiceProvider.GetRequiredService<IEquationBuilderExamples>();
var firstAttackExamples = scope.ServiceProvider.GetRequiredService<IFirstAlgorithmExamples>();
var secondAttackExamples = scope.ServiceProvider.GetRequiredService<ISecondAlgorithmExamples>();

secondAttackExamples.Example3().Wait();

Console.ReadLine();