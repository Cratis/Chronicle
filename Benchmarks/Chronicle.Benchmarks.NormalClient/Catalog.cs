// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>The application-sized artifact set, created once outside any measured window.</summary>
public sealed class Catalog
{
    Catalog(IReadOnlyList<Type> events, IReadOnlyList<Type> reducers, IReadOnlyList<Type> projections)
    {
        Events = events;
        Reducers = reducers;
        Projections = projections;
    }

    /// <summary>Gets every event type.</summary>
    public IReadOnlyList<Type> Events { get; }

    /// <summary>Gets the classic and self-reducing reducer types.</summary>
    public IReadOnlyList<Type> Reducers { get; }

    /// <summary>Gets the fluent projection types.</summary>
    public IReadOnlyList<Type> Projections { get; }

    /// <summary>Creates a catalog: half classic reducers, half self-reducing models, and fluent projections.</summary>
    /// <param name="eventCount">An even number of events, at least two.</param>
    /// <param name="projectionCount">How many fluent projections to create.</param>
    /// <returns>The catalog.</returns>
    public static Catalog Create(int eventCount, int projectionCount)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"NormalClientCatalog{eventCount}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Events");
        var events = Enumerable.Range(0, eventCount).Select(index => CreateEvent(module, index)).ToArray();
        var reducers = events.Take(eventCount / 2).Select(type => typeof(ClassicReducer<>).MakeGenericType(type))
            .Concat(events.Skip(eventCount / 2).Select((type, index) => CreateSelfModel(module, type, index))).ToArray();
        var projections = events.Take(projectionCount).Select(type => typeof(FluentProjection<>).MakeGenericType(type)).ToArray();
        return new(events, reducers, projections);
    }

    static Type CreateEvent(ModuleBuilder module, int index)
    {
        var builder = module.DefineType($"NormalEvent{index}", TypeAttributes.Public | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var constructor = typeof(EventTypeAttribute).GetConstructor([typeof(string), typeof(uint)])!;
        builder.SetCustomAttribute(new CustomAttributeBuilder(constructor, [$"normal-client-{index}", 1U]));
        return builder.CreateType();
    }

    static Type CreateSelfModel(ModuleBuilder module, Type eventType, int index)
    {
        var builder = module.DefineType($"NormalSelfModel{index}", TypeAttributes.Public | TypeAttributes.Sealed);
        var constructor = builder.DefineDefaultConstructor(MethodAttributes.Public);
        var method = builder.DefineMethod("Reduce", MethodAttributes.Public | MethodAttributes.Static, builder, [eventType, builder]);
        var body = method.GetILGenerator();
        body.Emit(OpCodes.Newobj, constructor);
        body.Emit(OpCodes.Ret);
        return builder.CreateType();
    }
}
