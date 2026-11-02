// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

/// <summary>
/// Manufactures collectible types, a registry that holds nothing but its types, and probes for observing unloading.
/// </summary>
public static class CollectibleDispatchTypes
{
    /// <summary>
    /// A non-collectible interface the collectible event types implement, so a non-collectible reducer can dispatch to them.
    /// </summary>
    public interface IKnown;

    /// <summary>
    /// Defines a collectible event type implementing <see cref="IKnown"/> in its own collectible assembly.
    /// </summary>
    /// <returns>The event type.</returns>
    public static Type DefineEvent() => DefineModule().DefineType("CollectibleEvent", TypeAttributes.Public, typeof(object), [typeof(IKnown)]).CreateType();

    /// <summary>
    /// Defines a collectible module that unloads once nothing references its types.
    /// </summary>
    /// <returns>The module.</returns>
    public static ModuleBuilder DefineModule() =>
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"collectible-{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("collectible");

    /// <summary>
    /// Runs the collector until every probe is dead or the attempts are exhausted.
    /// </summary>
    /// <param name="probes">The weak references to wait for.</param>
    /// <returns>True when every probe was collected.</returns>
    public static bool Collect(params WeakReference[] probes)
    {
        for (var attempt = 0; attempt < 20 && probes.Any(probe => probe.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return probes.All(probe => !probe.IsAlive);
    }

    /// <summary>
    /// A registry that references nothing except the types it was given, and records nothing about calls.
    /// </summary>
    /// <param name="types">The CLR types in the registry.</param>
    public sealed class Registry(IEnumerable<Type> types) : IEventTypes
    {
        /// <inheritdoc/>
        public IImmutableList<Type> AllClrTypes { get; } = [.. types];

        /// <inheritdoc/>
        public IImmutableList<EventType> All => throw new NotSupportedException();

        /// <inheritdoc/>
        public Task Discover() => throw new NotSupportedException();

        /// <inheritdoc/>
        public Task Register() => throw new NotSupportedException();

        /// <inheritdoc/>
        public bool HasFor(EventTypeId eventTypeId) => throw new NotSupportedException();

        /// <inheritdoc/>
        public Type GetClrTypeFor(EventTypeId eventTypeId) => throw new NotSupportedException();

        /// <inheritdoc/>
        public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => throw new NotSupportedException();

        /// <inheritdoc/>
        public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => throw new NotSupportedException();

        /// <inheritdoc/>
        public bool HasFor(Type clrType) => AllClrTypes.Contains(clrType);

        /// <inheritdoc/>
        public EventType GetEventTypeFor(Type clrType) => new(clrType.Name, 1);
    }
}
