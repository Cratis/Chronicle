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
    /// Creates 128 event types, 64 classic reducers and 64 self-reducing models.
    /// </summary>
    /// <returns>The application artifacts.</returns>
    public static InMemoryClientArtifactsProvider Create()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ClientStartupCatalog"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Events");
        var events = Enumerable.Range(1, 127).Select(index => CreateEvent(module, index)).Prepend(typeof(CatalogRecorded)).ToArray();
        return new InMemoryClientArtifactsProvider
        {
            EventTypes = events,
            Reducers = events.Take(64).Select(type => typeof(CatalogReducer<>).MakeGenericType(type))
                .Concat(events.Skip(64).Select((type, index) => CatalogSelfModel.Create(module, type, index))).ToArray()
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
