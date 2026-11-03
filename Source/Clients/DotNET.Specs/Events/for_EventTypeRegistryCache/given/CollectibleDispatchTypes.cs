// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
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
    /// Loads an event type implementing <see cref="IKnown"/> into its own collectible <see cref="AssemblyLoadContext"/>.
    /// </summary>
    /// <returns>The event type and the context to unload.</returns>
    public static (Type EventType, AssemblyLoadContext Context) LoadEventInUnloadableContext()
    {
        var name = $"unloadable-{Guid.NewGuid():N}";
        var builder = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
        builder.DefineDynamicModule(name).DefineType("UnloadableEvent", TypeAttributes.Public, typeof(object), [typeof(IKnown)]).CreateType();
        using var image = new MemoryStream();
        builder.Save(image);
        image.Position = 0;
        var context = new AssemblyLoadContext(name, isCollectible: true);
        return (context.LoadFromStream(image).GetType("UnloadableEvent", throwOnError: true)!, context);
    }

    /// <summary>
    /// Runs the collector until every probe is dead or the time budget is spent.
    /// </summary>
    /// <param name="probes">The weak references to wait for.</param>
    /// <returns>True when every probe was collected.</returns>
    /// <remarks>
    /// Unloading an <see cref="AssemblyLoadContext"/> takes several collections and finalizer passes, and the runtime
    /// postpones it while other threads are busy, so a fixed number of attempts is not enough in a parallel spec run.
    /// </remarks>
    public static bool Collect(params WeakReference[] probes)
    {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        while (probes.Any(probe => probe.IsAlive) && budget.Elapsed < TimeSpan.FromSeconds(10))
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1);
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
