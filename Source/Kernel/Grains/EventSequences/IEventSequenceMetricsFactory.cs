// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines a system that can create instances of <see cref="IEventSequenceMetrics"/>.
/// </summary>
public interface IEventSequenceMetricsFactory
{
    IEventSequenceMetrics CreateFor(EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);
}
