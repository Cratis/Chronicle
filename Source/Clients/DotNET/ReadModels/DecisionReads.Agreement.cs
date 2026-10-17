// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels;

public sealed partial class DecisionReads
{
    readonly ConcurrentDictionary<(Type Type, bool StreamScoped), Lazy<Task>> _agreements = new();

    static IEnumerable<string> GetAgreementDifferences(ReadModelDefinition model, ProjectionDefinition kernel, ProjectionDefinition client, EventType[] types)
    {
        if (model.ObserverIdentifier != client.Identifier) yield return nameof(model.ObserverIdentifier);
        if (model.ObserverType != ReadModelObserverType.Projection) yield return nameof(model.ObserverType);
        if (kernel.ReadModel != client.ReadModel) yield return nameof(kernel.ReadModel);
        if (kernel.EventSequenceId != client.EventSequenceId) yield return nameof(kernel.EventSequenceId);
        if (kernel.GenerationDelivery != client.GenerationDelivery) yield return nameof(kernel.GenerationDelivery);
        if (kernel.SubscribesToAllEvents != client.SubscribesToAllEvents) yield return nameof(kernel.SubscribesToAllEvents);
        if (kernel.Join.Count != client.Join.Count) yield return nameof(kernel.Join);
        if (kernel.RemovedWithJoin.Count != client.RemovedWithJoin.Count) yield return nameof(kernel.RemovedWithJoin);
        if (kernel.Children.Count != client.Children.Count) yield return nameof(kernel.Children);
        if (kernel.Nested.Count != client.Nested.Count) yield return nameof(kernel.Nested);
        if (kernel.FromEvery.Count != client.FromEvery.Count) yield return nameof(kernel.FromEvery);
        if (kernel.FromEventProperty is not null) yield return nameof(kernel.FromEventProperty);
        if (!kernel.From.Keys.Concat(kernel.RemovedWith.Keys).Select(_ => _.Id).ToHashSet().SetEquals(types.Select(_ => _.Id.Value)))
        {
            yield return "EventTypes";
        }
        if (NormalizeKey(kernel.All.Key) != NormalizeKey(client.All.Key)) yield return "All.Key";
        if (kernel.From.Count != client.From.Count || kernel.From.Any(_ =>
            client.From.FirstOrDefault(pair => pair.Key.Id == _.Key.Id).Value is not { } from ||
            NormalizeKey(_.Value.Key) != NormalizeKey(from.Key) || NormalizeKey(_.Value.ParentKey) != NormalizeKey(from.ParentKey)))
        {
            yield return "From event types or key routing";
        }
        if (kernel.RemovedWith.Count != client.RemovedWith.Count || kernel.RemovedWith.Any(_ =>
            client.RemovedWith.FirstOrDefault(pair => pair.Key.Id == _.Key.Id).Value is not { } removed ||
            NormalizeKey(_.Value.Key) != NormalizeKey(removed.Key) || NormalizeKey(_.Value.ParentKey) != NormalizeKey(removed.ParentKey)))
        {
            yield return "RemovedWith event types or key routing";
        }
    }

    static string NormalizeKey(string? key) => string.IsNullOrEmpty(key) ? "$eventSourceId" : key;

    Task ClearAgreements()
    {
        _agreements.Clear();
        return Task.CompletedTask;
    }

    Lazy<Task> CreateAgreement((Type Type, bool StreamScoped) key, ProjectionDefinition definition, EventType[] types)
    {
        Lazy<Task> agreement = null!;
        agreement = new(() => CheckAndRetainAgreement(key, agreement, definition, types));
        return agreement;
    }

    async Task CheckAndRetainAgreement((Type Type, bool StreamScoped) key, Lazy<Task> agreement, ProjectionDefinition definition, EventType[] types)
    {
        try
        {
            try
            {
                await CheckAgreement(key.Type, definition, types);
            }
            catch (DecisionReadRefused)
            {
                // Definitions may have arrived after the first listing. Re-check once, without re-registering them.
                await CheckAgreement(key.Type, definition, types);
            }
        }
        catch
        {
            // The lazy is inserted before it starts, so even synchronous failures are evicted.
            // Remove only this attempt: a lifecycle event may already have installed a newer one.
            ((ICollection<KeyValuePair<(Type, bool), Lazy<Task>>>)_agreements).Remove(new(key, agreement));
            throw;
        }
    }

    async Task CheckAgreement(Type type, ProjectionDefinition client, EventType[] types)
    {
        var services = ((IChronicleServicesAccessor)_eventStore.Connection).Services;
        var context = CallContext.Default;
        var readModels = await services.ReadModels.GetDefinitions(new() { EventStore = _eventStore.Name }, context);
        var matches = readModels.ReadModels.Where(_ => _.Type.Identifier == type.GetReadModelIdentifier()).ToArray();
        var definitions = await services.Projections.GetAllDefinitions(new() { EventStore = _eventStore.Name }, context);
        var projections = definitions.Where(_ => _.Identifier == client.Identifier).ToArray();
        if (matches.Length == 0 || projections.Length == 0)
        {
            var missing = matches.Length == 0 ? "read-model" : "projection";
            throw new DecisionReadRefused(DecisionReadRefusalReason.DefinitionMissing, type, $"The {missing} definition is missing on the kernel.");
        }
        if (matches.Length != 1 || projections.Length != 1)
        {
            var duplicated = matches.Length != 1 ? "read-model" : "projection";
            throw new DecisionReadRefused(DecisionReadRefusalReason.DefinitionMismatch, type, $"The kernel lists multiple {duplicated} definitions.");
        }
        var differences = GetAgreementDifferences(matches[0], projections[0], client, types).ToArray();
        if (differences.Length != 0)
        {
            throw new DecisionReadRefused(DecisionReadRefusalReason.DefinitionMismatch, type, $"The kernel definition differs in: {string.Join(", ", differences)}.");
        }
    }
}
