using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NuminaBit.Services.Ciphers.DES;
using NuminaBit.Services.Ciphers.DES.Interfaces;

using NuminaBit.Services.Ciphers.Shared;
using NuminaBit.Services.Ciphers.Shared.Interfaces;

namespace NuminaBit.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNuminaBitServices(this IServiceCollection services)
        {

            services.TryAddSingleton<ICore, Core>();
            services.TryAddSingleton<ILAT, LatCalculator>();
            services.TryAddSingleton<IAttackRunner, AttackRunner>();
            services.TryAddSingleton<IAttackRunner2, AttackRunner2>();
            services.TryAddSingleton<IEquationBuilder, EquationBuilder>();

            return services;
        }
    }
}
