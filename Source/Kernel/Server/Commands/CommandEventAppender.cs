// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;
using ClientEventTypeAttribute = Cratis.Chronicle.Events.EventTypeAttribute;
using KernelEventSourceId = Cratis.Chronicle.Concepts.Events.EventSourceId;
using KernelEventTypeAttribute = Cratis.Chronicle.Concepts.Events.EventTypeAttribute;
using KeyAttribute = Cratis.Chronicle.Keys.KeyAttribute;

namespace Cratis.Chronicle.Server.Commands;

/// <summary>
/// Resolves the target event sequence and appends events emitted by a command return value, using the
/// existing <see cref="IEventSequences"/> gRPC service surface so type resolution stays unambiguous
/// across the kernel and client assemblies. The CLR event source is resolved from the originating
/// command via either an <c>EventSourceId</c>-typed property or a property marked with
/// <see cref="KeyAttribute"/>.
/// </summary>
/// <param name="eventSequences">The <see cref="IEventSequences"/> service used to append events.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> service used to lazily register workbench-internal event types.</param>
public class CommandEventAppender(IEventSequences eventSequences, IEventTypes eventTypes)
{
    static readonly ConcurrentDictionary<string, bool> _registeredEventTypes = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns true if the given value's CLR type is decorated with either the kernel or the
    /// client-SDK <c>EventType</c> attribute. Both are accepted because workbench-API events use the
    /// client-SDK attribute while kernel-internal events use the kernel one.
    /// </summary>
    /// <param name="value">The candidate value.</param>
    /// <returns>True if the value is a Chronicle event.</returns>
    public static bool IsEvent(object? value)
    {
        if (value is null) return false;
        var type = value.GetType();
        return Attribute.IsDefined(type, typeof(ClientEventTypeAttribute)) ||
               Attribute.IsDefined(type, typeof(KernelEventTypeAttribute));
    }

    /// <summary>
    /// Appends a single event to the appropriate event sequence resolved from the command and the event type.
    /// </summary>
    /// <param name="command">The originating command instance.</param>
    /// <param name="event">The event to append.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Append(object command, object @event)
    {
        var (eventTypeId, generation) = ResolveEventType(@event.GetType());
        var eventStore = ResolveEventStore(command);
        await EnsureEventTypeRegistered(eventStore, eventTypeId, generation);

        var request = new AppendRequest
        {
            EventStore = eventStore,
            Namespace = ResolveNamespace(command),
            EventSequenceId = WellKnownEventSequences.EventLog,
            EventSourceType = string.Empty,
            EventSourceId = ResolveEventSourceId(command),
            EventStreamType = string.Empty,
            EventStreamId = string.Empty,
            EventType = new Contracts.Events.EventType { Id = eventTypeId, Generation = generation },
            Content = JsonSerializer.Serialize(@event, @event.GetType()),
            ConcurrencyScope = new ConcurrencyScope { SequenceNumber = ulong.MaxValue, EventSourceId = false },
            Causation = [],
            CausedBy = new Contracts.Identities.Identity { Subject = "workbench", Name = "Workbench", UserName = "workbench" },
            Tags = []
        };

        await eventSequences.Append(request);
    }

    static (string Id, uint Generation) ResolveEventType(Type type)
    {
        var clientAttribute = type.GetCustomAttribute<ClientEventTypeAttribute>();
        if (clientAttribute is not null)
        {
            var id = string.IsNullOrEmpty((string)clientAttribute.Id) ? type.Name : (string)clientAttribute.Id;
            return (id, (uint)clientAttribute.Generation);
        }

        var kernelAttribute = type.GetCustomAttribute<KernelEventTypeAttribute>();
        if (kernelAttribute is not null)
        {
            var id = string.IsNullOrEmpty((string)kernelAttribute.Id) ? type.Name : (string)kernelAttribute.Id;
            return (id, (uint)kernelAttribute.Generation);
        }

        return (type.Name, 1);
    }

    static string ResolveEventSourceId(object command)
    {
        var commandType = command.GetType();
        var property = commandType.GetProperties().FirstOrDefault(p =>
            p.PropertyType == typeof(KernelEventSourceId) ||
            Attribute.IsDefined(p, typeof(KeyAttribute)));

        var value = property?.GetValue(command) ?? throw new MissingEventSourceIdOnCommand(commandType);
        return UnwrapToString(value);
    }

    static string UnwrapToString(object value)
    {
        var valueType = value.GetType();
        var inner = valueType.GetProperty("Value");
        if (inner is not null && (inner.PropertyType == typeof(Guid) || inner.PropertyType == typeof(string)))
        {
            var innerValue = inner.GetValue(value);
            if (innerValue is not null)
            {
                return innerValue.ToString()!;
            }
        }

        return value.ToString()!;
    }

    static string ResolveEventStore(object command)
    {
        var value = command.GetType().GetProperty("EventStore")?.GetValue(command)?.ToString();
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    static string ResolveNamespace(object command)
    {
        var value = command.GetType().GetProperty("Namespace")?.GetValue(command)?.ToString();
        return string.IsNullOrEmpty(value) ? "default" : value;
    }

    async Task EnsureEventTypeRegistered(string eventStore, string eventTypeId, uint generation)
    {
        var key = $"{eventStore}|{eventTypeId}|{generation}";
        if (_registeredEventTypes.ContainsKey(key))
        {
            return;
        }

        await eventTypes.Register(new RegisterEventTypesRequest
        {
            EventStore = eventStore,
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new Contracts.Events.EventType { Id = eventTypeId, Generation = generation },
                    Schema = "{\"type\":\"object\"}",
                    Generations =
                    [
                        new EventTypeGenerationDefinition
                        {
                            Generation = generation,
                            Schema = "{\"type\":\"object\"}"
                        }
                    ]
                }
            ]
        });

        _registeredEventTypes.TryAdd(key, true);
    }
}
