// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Prepares and registers the store-local trigger contracts and their handlers.
/// </summary>
/// <param name="eventStore">The owning event store.</param>
/// <param name="artifacts">The store-local artifact catalog.</param>
/// <param name="schemas">The payload schema generator.</param>
internal sealed class TriggerRegistry(IEventStore eventStore, IClientArtifactsProvider artifacts, IJsonSchemaGenerator schemas)
{
    FrozenDictionary<Type, TriggerRegistration> _registrations = FrozenDictionary<Type, TriggerRegistration>.Empty;
    FrozenDictionary<Type, Type> _handlers = FrozenDictionary<Type, Type>.Empty;

    /// <summary>
    /// Gets the discovered trigger and handler pairs.
    /// </summary>
    internal IEnumerable<(Type Trigger, Type Handler)> Handlers => _handlers.Select(_ => (_.Key, _.Value));

    /// <summary>
    /// Gets whether this store has any lifecycle handlers.
    /// </summary>
    internal bool HasLifecycleHandlers => _handlers.Keys.Any(IsLifecycle);

    /// <summary>
    /// Determines whether a payload is a built-in lifecycle trigger.
    /// </summary>
    /// <param name="type">The payload type.</param>
    /// <returns>Whether this is Startup or Shutdown.</returns>
    internal static bool IsLifecycle(Type type) => type == typeof(Startup) || type == typeof(Shutdown);

    /// <summary>
    /// Builds an immutable catalog, rejecting duplicate or invalid declarations before publishing it.
    /// </summary>
    /// <exception cref="TriggerNotValid">A declaration is invalid, duplicated or missing its trigger.</exception>
    internal void Discover()
    {
        var builtIns = artifacts.OccurrenceHandlers.SelectMany(_ => _.GetInterfaces()).Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IOccurrenceHandler<>)).Select(_ => _.GenericTypeArguments[0]).Where(IsLifecycle).ToArray();
        var triggers = artifacts.Triggers.Concat(builtIns.Length > 0 ? [typeof(Startup), typeof(Shutdown)] : []).Distinct().ToArray();
        var registrations = triggers.ToDictionary(type => type, type =>
        {
            var marker = type.GetCustomAttribute<TriggerAttribute>();
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters || marker is null || string.IsNullOrWhiteSpace(marker.Id.Value))
            {
                throw new TriggerNotValid($"Trigger '{type.FullName}' must be a concrete closed type with a non-empty Trigger identity.");
            }
            return new TriggerRegistration { Id = marker.Id, Name = type.Name, Schema = schemas.Generate(type).ToJson() };
        });
        var duplicate = registrations.Values.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Count() > 1);
        if (duplicate is not null) throw new TriggerNotValid($"Multiple trigger types declare '{duplicate.Key}'.");

        var handlers = new Dictionary<Type, Type>();
        foreach (var handler in artifacts.OccurrenceHandlers.Distinct())
        {
            var targets = handler.GetInterfaces().Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IOccurrenceHandler<>)).ToArray();
            if (!handler.IsClass || handler.IsAbstract || handler.ContainsGenericParameters || targets.Length != 1)
            {
                throw new TriggerNotValid($"Handler '{handler.FullName}' must implement exactly one closed IOccurrenceHandler<TTrigger>.");
            }
            var trigger = targets[0].GenericTypeArguments[0];
            if (!registrations.ContainsKey(trigger) || !handlers.TryAdd(trigger, handler))
            {
                throw new TriggerNotValid($"Handler '{handler.FullName}' must target a discovered trigger without another handler.");
            }
        }
        _registrations = registrations.ToFrozenDictionary();
        _handlers = handlers.ToFrozenDictionary();
    }

    /// <summary>
    /// Resolves a discovered trigger's registration.
    /// </summary>
    /// <param name="type">The payload type.</param>
    /// <returns>The prepared registration.</returns>
    /// <exception cref="TriggerNotValid">The type is not in the prepared catalog.</exception>
    internal TriggerRegistration Get(Type type) => _registrations.TryGetValue(type, out var registration)
        ? registration
        : throw new TriggerNotValid($"Trigger '{type.FullName}' has not been discovered in this event store.");

    /// <summary>
    /// Registers prepared schemas with an explicitly capable kernel.
    /// </summary>
    /// <returns>The registration work.</returns>
    /// <exception cref="TriggerNotValid">The kernel refused registration.</exception>
    internal async Task Register()
    {
        if (_registrations.Count == 0) return;
        OccurrenceSupport.Require(eventStore.Connection);
        var result = await ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences.RegisterTriggers(new()
        {
            EventStore = eventStore.Name,
            Triggers = _registrations.Values.ToArray()
        }).EnsureSuccess();
        if (!result.Registered) throw new TriggerNotValid(string.Join(Environment.NewLine, result.Errors));
    }
}
