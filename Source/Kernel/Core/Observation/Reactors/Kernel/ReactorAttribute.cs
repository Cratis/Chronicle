// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an Reactor.
/// </summary>
/// <param name="id">Optional <see cref="Id"/> represented as string, if not used it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
/// <param name="systemEventStoreOnly">Optional value indicating whether this reactor is a reactor for the system event store only. Defaults to true.</param>
/// <param name="defaultNamespaceOnly">Optional value indicating whether this reactor is limited to the default namespace. Defaults to true.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReactorAttribute(string id = "", string? eventSequence = default, bool systemEventStoreOnly = true, bool defaultNamespaceOnly = true) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for an Reactor.
    /// </summary>
    public ReactorId Id { get; } = id;

    /// <summary>
    /// Gets the unique identifier for an event log.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequence ?? EventSequenceId.Log;

    /// <summary>
    /// Gets a value indicating whether this reactor is a system reactor.
    /// </summary>
    /// <remarks>
    /// System reactors lives in the system event store only. This is the default behavior.
    /// </remarks>
    public bool IsSystemEventStoreOnly { get; } = systemEventStoreOnly;

    /// <summary>
    /// Gets a value indicating whether this reactor is limited to the default namespace.
    /// </summary>
    /// <remarks>
    /// This indicates that the reactor will only be registered in the default namespace of an event store.
    /// </remarks>
    public bool DefaultNamespaceOnly { get; } = defaultNamespaceOnly;
}
