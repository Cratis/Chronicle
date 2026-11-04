// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Testing;

public partial class KernelArtifactExclusion
{
    /// <summary>
    /// Fails startup when a provider collection registered after the Chronicle client replaced the excluding one.
    /// </summary>
    /// <remarks>
    /// The last registration of a service wins, so a later closed collection would silently bring the kernel artifacts back.
    /// Arc resolves the collections through its aggregates only, which is why the check is skipped when Arc is absent.
    /// </remarks>
    /// <param name="services">The application's service provider.</param>
    sealed class StartupCheck(IServiceProvider services) : IHostedLifecycleService
    {
        public Task StartingAsync(CancellationToken cancellationToken)
        {
            Verify<ICommandHandlerProvider, ICommandHandlerProviders>();
            Verify<IQueryPerformerProvider, IQueryPerformerProviders>();
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        void Verify<TProvider, TAggregate>()
            where TProvider : class
        {
            if (services.GetService<IServiceProviderIsService>()?.IsService(typeof(TAggregate)) != true)
            {
                return;
            }

            if (services.GetService<IInstancesOf<TProvider>>() is not ProviderInstances<TProvider>)
            {
                throw CustomProviderCollection(typeof(IInstancesOf<TProvider>));
            }
        }
    }
}
