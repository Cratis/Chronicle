// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Creates the fixed catalog outside the measured window.
/// </summary>
public static class StartupCatalog
{
    /// <summary>
    /// Creates an application-sized event catalog split between classic reducers and self-reducing models.
    /// </summary>
    /// <param name="eventCount">The even number of event types to create.</param>
    /// <returns>The application artifacts.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is less than two or is odd.</exception>
    public static InMemoryClientArtifactsProvider Create(int eventCount = 128)
    {
        if (eventCount < 2 || eventCount % 2 != 0) throw new ArgumentOutOfRangeException(nameof(eventCount));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ClientStartupCatalog{eventCount}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Events");
        var events = Enumerable.Range(1, eventCount - 1).Select(index => CreateEvent(module, index)).Prepend(typeof(CatalogRecorded)).ToArray();
        return new InMemoryClientArtifactsProvider
        {
            EventTypes = events,
            Reducers = events.Take(eventCount / 2).Select(type => typeof(CatalogReducer<>).MakeGenericType(type))
                .Concat(events.Skip(eventCount / 2).Select((type, index) => CatalogSelfModel.Create(module, type, index))).ToArray()
        };
    }

    static Type CreateEvent(ModuleBuilder module, int index)
    {
        var builder = module.DefineType($"CatalogEvent{index}", TypeAttributes.Public | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var constructor = typeof(EventTypeAttribute).GetConstructor([typeof(string), typeof(uint)])!;
        builder.SetCustomAttribute(new CustomAttributeBuilder(constructor, [$"client-startup-{index}", 1U]));
        return builder.CreateType()!;
    }
}
