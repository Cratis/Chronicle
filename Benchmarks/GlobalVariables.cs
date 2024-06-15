// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

internal static class GlobalVariables
{
    internal const string ObserverEventSequence = "141094c6-3f6f-4d4b-989d-0541c02bdfea";

    internal static readonly IEnumerable<Causation> BenchmarkCausation = new[] { new Causation(DateTimeOffset.UtcNow, "Benchmark", new Dictionary<string, string>()) };
    internal static readonly MicroserviceId MicroserviceId = MicroserviceId.Kernel;
    internal static readonly TenantId TenantId = Guid.Empty;
    internal static IServiceProvider ServiceProvider { get; private set; } = new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());

    internal static void SetServiceProvider(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
}
