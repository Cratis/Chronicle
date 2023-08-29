// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

internal static class GlobalVariables
{
    internal static readonly IEnumerable<Causation> BenchmarkCausation = new[] { new Causation(DateTimeOffset.UtcNow, "Benchmark", new Dictionary<string, string>()) };
    internal static readonly MicroserviceId MicroserviceId = "95166a40-617f-4c40-ae8c-3337d7f9c59c";
    internal static IServiceProvider ServiceProvider { get; private set; } = new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());

    internal static void SetServiceProvider(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
}
