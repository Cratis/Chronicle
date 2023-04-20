// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceMetricsFactory"/>.
/// </summary>
public class EventSequenceMetricsFactory : IEventSequenceMetricsFactory
{
    readonly Meter _meter;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequenceMetricsFactory"/>.
    /// </summary>
    /// <param name="meter">The global <see cref="Meter"/>.</param>
    public EventSequenceMetricsFactory(Meter meter) => _meter = meter;

    /// <inheritdoc/>
    public IEventSequenceMetrics CreateFor(EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId) => new EventSequenceMetrics(_meter, eventSequenceId, microserviceId, tenantId);
}
