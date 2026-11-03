// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the implementation of <see cref="IResourceNameGate"/>.
/// </summary>
/// <param name="snapshot">The <see cref="IResourceNameSnapshot"/>.</param>
/// <param name="reservations">The <see cref="IResourceNameReservations"/>.</param>
/// <param name="exemptions">The <see cref="ResourceNameExemptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{T}"/>.</param>
/// <param name="physicalNames">The optional <see cref="IResourcePhysicalNames"/> of the backend.</param>
public class ResourceNameGate(
    IResourceNameSnapshot snapshot,
    IResourceNameReservations reservations,
    ResourceNameExemptions exemptions,
    ILogger<ResourceNameGate> logger,
    IResourcePhysicalNames? physicalNames = null) : IResourceNameGate
{
    const int MaximumLoggedLength = 128;

    readonly ConcurrentDictionary<(string EventStore, string? Namespace), byte> _registered = new();

    /// <inheritdoc/>
    public void EnsureAvailable(EventStoreName eventStore)
    {
        if (exemptions.IsExempt(eventStore.Value))
        {
            return;
        }

        ThrowIfUnavailable(snapshot.Check(eventStore.Value), eventStore.Value);
    }

    /// <inheritdoc/>
    public void EnsureAvailable(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (exemptions.IsExempt(eventStore.Value, @namespace.Value))
        {
            return;
        }

        ThrowIfUnavailable(snapshot.Check(eventStore.Value, @namespace.Value), $"{eventStore.Value}#{@namespace.Value}");
    }

    /// <inheritdoc/>
    public void EnsureCanOpen(EventStoreName eventStore)
    {
        if (exemptions.IsExempt(eventStore.Value))
        {
            return;
        }

        var violation = ResourceNames.ForEventStore(eventStore.Value);
        if (violation != ResourceNameViolation.None && !IsRegistered(eventStore.Value, null))
        {
            throw Refuse(violation, eventStore.Value, InvalidResourceName.ForEventStore(violation));
        }

        ThrowIfUnavailable(snapshot.Check(eventStore.Value), eventStore.Value);
    }

    /// <inheritdoc/>
    public void EnsureCanOpen(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (exemptions.IsExempt(eventStore.Value, @namespace.Value))
        {
            return;
        }

        var combination = ResourceNames.ForCombination(eventStore.Value, @namespace.Value);
        if (combination != ResourceNameViolation.None)
        {
            throw Refuse(combination, $"{eventStore.Value}#{@namespace.Value}", InvalidResourceName.ForNamespace(combination));
        }

        var storeViolation = exemptions.IsExempt(eventStore.Value) ? ResourceNameViolation.None : ResourceNames.ForEventStore(eventStore.Value);
        if (storeViolation != ResourceNameViolation.None && !IsRegistered(eventStore.Value, null))
        {
            throw Refuse(storeViolation, eventStore.Value, InvalidResourceName.ForEventStore(storeViolation));
        }

        var namespaceViolation = ResourceNames.ForNamespace(@namespace.Value);
        if (namespaceViolation != ResourceNameViolation.None && !IsRegistered(eventStore.Value, @namespace.Value))
        {
            throw Refuse(namespaceViolation, @namespace.Value, InvalidResourceName.ForNamespace(namespaceViolation));
        }

        ThrowIfUnavailable(snapshot.Check(eventStore.Value, @namespace.Value), $"{eventStore.Value}#{@namespace.Value}");
    }

    /// <inheritdoc/>
    public void MarkRegistered(EventStoreName eventStore) => _registered.TryAdd((eventStore.Value, null), 0);

    /// <inheritdoc/>
    public void MarkRegistered(EventStoreName eventStore, EventStoreNamespaceName @namespace) => _registered.TryAdd((eventStore.Value, @namespace.Value), 0);

    /// <inheritdoc/>
    public async Task EnsureCanCreate(ResourceCreation creation)
    {
        var eventStore = creation.EventStore.Value;
        var @namespace = creation.Namespace?.Value;

        if (@namespace is null ? exemptions.IsExempt(eventStore) : exemptions.IsExempt(eventStore, @namespace))
        {
            return;
        }

        EnsureRule(eventStore, @namespace);
        ThrowIfUnavailable(snapshot.Check(eventStore, @namespace), Owner(eventStore, @namespace));

        var name = @namespace ?? eventStore;
        if (creation.RegisteredNames.Any(_ => _ == name))
        {
            return;
        }

        if (creation.RegisteredNames.Any(_ => string.Equals(_, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw Conflict(@namespace is null ? ResourceNameConflictKind.EventStoreCaseVariant : ResourceNameConflictKind.NamespaceCaseVariant, name);
        }

        if (await Overlaps(creation))
        {
            throw Conflict(ResourceNameConflictKind.Overlap, name);
        }

        var physical = physicalNames?.For(creation.EventStore, creation.Namespace) ?? [];
        if (physicalNames is not null && physical.Count > 0 && await physicalNames.AnyExists(physical))
        {
            throw Conflict(ResourceNameConflictKind.Overlap, name);
        }

        if (!await reservations.Reserve(Owner(eventStore, @namespace), Keys(creation, physical)))
        {
            throw Conflict(ResourceNameConflictKind.Overlap, name);
        }

        if (@namespace is null)
        {
            MarkRegistered(creation.EventStore);
        }
        else
        {
            MarkRegistered(creation.EventStore, creation.Namespace!);
        }
    }

    static Task<bool> Overlaps(ResourceCreation creation)
    {
        if (creation.Namespace is null)
        {
            return Task.FromResult(ResourceNameOverlaps.ForEventStore(creation.EventStore.Value, creation.RegisteredNames));
        }

        return ResourceNameOverlaps.ForNamespace(
            creation.EventStore.Value,
            creation.Namespace.Value,
            creation.RegisteredNames,
            creation.RegisteredEventStores ?? [],
            creation.NamespacesOf ?? (_ => Task.FromResult<IEnumerable<string>>([])));
    }

    static string Owner(string eventStore, string? @namespace) => @namespace is null ? eventStore : $"{eventStore}#{@namespace}";

    static string Fold(string value) => value.ToUpperInvariant();

    static string[] Keys(ResourceCreation creation, IReadOnlyCollection<string> physical) =>
    [
        creation.Namespace is null
            ? $"store:{Fold(creation.EventStore.Value)}"
            : $"ns:{Fold(creation.EventStore.Value)}#{Fold(creation.Namespace.Value)}",
        .. physical.Select(_ => $"res:{Fold(_)}").Distinct()
    ];

    static string Escape(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Length > MaximumLoggedLength ? value[..MaximumLoggedLength] : value)
        {
            if (character is >= ' ' and <= '~')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    void EnsureRule(string eventStore, string? @namespace)
    {
        var storeViolation = ResourceNames.ForEventStore(eventStore);
        if (storeViolation != ResourceNameViolation.None && !(@namespace is not null && (exemptions.IsExempt(eventStore) || IsRegistered(eventStore, null))))
        {
            throw Refuse(storeViolation, eventStore, InvalidResourceName.ForEventStore(storeViolation));
        }

        if (@namespace is null)
        {
            return;
        }

        var namespaceViolation = ResourceNames.ForNamespace(@namespace);
        if (namespaceViolation == ResourceNameViolation.None)
        {
            namespaceViolation = ResourceNames.ForCombination(eventStore, @namespace);
        }

        if (namespaceViolation != ResourceNameViolation.None)
        {
            throw Refuse(namespaceViolation, @namespace, InvalidResourceName.ForNamespace(namespaceViolation));
        }
    }

    bool IsRegistered(string eventStore, string? @namespace) => _registered.ContainsKey((eventStore, @namespace));

    void ThrowIfUnavailable(ResourceNameStatus status, string name)
    {
        switch (status)
        {
            case ResourceNameStatus.NotReady:
                throw new ResourceNamesNotReady();
            case ResourceNameStatus.Quarantined:
                logger.NameQuarantined(Escape(name));
                throw new ResourceQuarantined();
        }
    }

    InvalidResourceName Refuse(ResourceNameViolation violation, string name, InvalidResourceName exception)
    {
        logger.NameRefused(violation, Escape(name));
        return exception;
    }

    ResourceNameConflict Conflict(ResourceNameConflictKind kind, string name)
    {
        logger.NameConflict(kind, Escape(name));
        return new ResourceNameConflict(kind);
    }
}
