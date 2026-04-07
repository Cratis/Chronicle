// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-process implementation of <see cref="IServices"/> for test scenarios.
/// </summary>
/// <remarks>
/// Only <see cref="EventSequences"/> and <see cref="Constraints"/> are provided with real
/// implementations. All other service properties throw <see cref="NotSupportedException"/>
/// since they are not needed for event-sequence testing.
/// </remarks>
/// <param name="eventSequences">The <see cref="IEventSequences"/> service.</param>
/// <param name="constraints">The <see cref="IConstraints"/> service.</param>
internal sealed class InProcessServices(
    IEventSequences eventSequences,
    IConstraints constraints) : IServices
{
    /// <inheritdoc/>
    public IEventSequences EventSequences => eventSequences;

    /// <inheritdoc/>
    public IConstraints Constraints => constraints;

    /// <inheritdoc/>
    public IEventStores EventStores => throw new NotSupportedException("EventStores is not supported in test scenarios.");

    /// <inheritdoc/>
    public INamespaces Namespaces => throw new NotSupportedException("Namespaces is not supported in test scenarios.");

    /// <inheritdoc/>
    public IRecommendations Recommendations => throw new NotSupportedException("Recommendations is not supported in test scenarios.");

    /// <inheritdoc/>
    public IIdentities Identities => throw new NotSupportedException("Identities is not supported in test scenarios.");

    /// <inheritdoc/>
    public IEventTypes EventTypes => throw new NotSupportedException("EventTypes is not supported in test scenarios.");

    /// <inheritdoc/>
    public IObservers Observers => throw new NotSupportedException("Observers is not supported in test scenarios.");

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => throw new NotSupportedException("FailedPartitions is not supported in test scenarios.");

    /// <inheritdoc/>
    public IReactors Reactors => throw new NotSupportedException("Reactors is not supported in test scenarios.");

    /// <inheritdoc/>
    public IReducers Reducers => throw new NotSupportedException("Reducers is not supported in test scenarios.");

    /// <inheritdoc/>
    public IProjections Projections => throw new NotSupportedException("Projections is not supported in test scenarios.");

    /// <inheritdoc/>
    public IWebhooks Webhooks => throw new NotSupportedException("Webhooks is not supported in test scenarios.");

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => throw new NotSupportedException("EventStoreSubscriptions is not supported in test scenarios.");

    /// <inheritdoc/>
    public IReadModels ReadModels => throw new NotSupportedException("ReadModels is not supported in test scenarios.");

    /// <inheritdoc/>
    public IJobs Jobs => throw new NotSupportedException("Jobs is not supported in test scenarios.");

    /// <inheritdoc/>
    public IEventSeeding Seeding => throw new NotSupportedException("Seeding is not supported in test scenarios.");

    /// <inheritdoc/>
    public IUsers Users => throw new NotSupportedException("Users is not supported in test scenarios.");

    /// <inheritdoc/>
    public IApplications Applications => throw new NotSupportedException("Applications is not supported in test scenarios.");

    /// <inheritdoc/>
    public IServer Server => throw new NotSupportedException("Server is not supported in test scenarios.");
}
