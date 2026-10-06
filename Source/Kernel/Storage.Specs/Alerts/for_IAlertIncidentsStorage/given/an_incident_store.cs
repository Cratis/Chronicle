// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.given;

/// <summary>
/// Provides a real, isolated storage provider and recorded transitions.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public class an_incident_store<THarness> : Specification
    where THarness : IAlertIncidentsStorageHarness, new()
{
    protected static readonly DateTimeOffset _occurred = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
    protected readonly IncidentId _id = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    protected IAlertIncidentsStorage _storage;
    THarness _harness;

    async Task Establish()
    {
        _harness = CreateHarness();
        _storage = await _harness.Create();
    }

    async Task Destroy() => await _harness.DisposeAsync();

    /// <summary>
    /// Creates the provider harness, optionally with its infrastructure fixture.
    /// </summary>
    /// <returns>The provider harness.</returns>
    protected virtual THarness CreateHarness() => new();

    /// <summary>
    /// Creates a recorded raise with complete, losslessly preserved evidence.
    /// </summary>
    /// <param name="number">Sequence position.</param>
    /// <param name="id">Optional identity.</param>
    /// <param name="store">Affected store.</param>
    /// <param name="namespace">Affected namespace.</param>
    /// <returns>The recorded raise.</returns>
#pragma warning disable CA1030 // This specification factory creates recorded data, not an event subscription.
    protected AlertIncidentTransition Raise(ulong number, IncidentId? id = null, string store = "store", string @namespace = "Default") => new(
        AlertIncidentTransitionKind.Raised,
        id ?? _id,
        new(store, @namespace, "observer", EventSequenceId.Log, "[none]"),
        AlertConditionKind.PartitionFailing,
        AlertSeverity.Warning,
        new(3, _occurred.AddTicks(-123), _occurred.AddTicks(-10), FailureKind.Handling, "recorded evidence"),
        null,
        _occurred.AddTicks((long)Math.Min(number, 100)),
        number);
#pragma warning restore CA1030
}
