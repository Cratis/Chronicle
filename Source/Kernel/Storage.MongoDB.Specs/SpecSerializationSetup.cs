// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Registers the same MongoDB serialization the real server uses, once, before any spec runs.
/// </summary>
/// <remarks>
/// Runs at assembly load (before any <see cref="BsonClassMap"/> is frozen) so document serialization in specs
/// is faithful to the silo: the camelCase element-name convention (the persisted field is "status", not
/// "Status") and concept serialization. Doing this in a spec base static constructor is too late once another
/// spec has already touched a type — a fidelity gap that let a serializer-dependent index bug slip past specs.
/// <para>
/// The server also registers <see cref="IgnoreExtraElementsConvention"/> through <c>AddCratisMongoDB</c>. Without
/// it, reading back any stored type that has no id member fails on the <c>_id</c> the server generated, which is a
/// failure specs would see and production would not.
/// </para>
/// <para>
/// The same reasoning applies to every <see cref="IBsonClassMapFor{T}"/>. Take <see cref="EventClassMap"/>, which is
/// what makes the event's sequence number the document <c>_id</c>: any spec that merely renders a
/// <c>Builders&lt;Event&gt;</c> expression — building an index key, for instance — makes the driver auto-register a
/// <em>default</em> class map for <see cref="Event"/>, and a default map gives every appended event a generated
/// <c>ObjectId</c> instead. A lazy <c>IsClassMapRegistered</c> guard in a spec base then finds one already
/// registered and skips the real one, so whichever spec touched the type first decides the mapping. That silently
/// dropped both the tail aggregation and the duplicate-sequence-number detection when spec classes ran in parallel.
/// </para>
/// <para>
/// So every <see cref="IBsonClassMapFor{T}"/> the server registers is registered here too, discovered the same way
/// <c>AddCratisMongoDB</c> discovers them, rather than one at a time from whichever spec first needs one.
/// </para>
/// </remarks>
internal static class SpecSerializationSetup
{
    /// <summary>
    /// Registers the server's MongoDB serialization conventions, providers and class maps.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        new ConventionPacks().Provide();
        ConventionRegistry.Register(Cratis.Arc.MongoDB.ConventionPacks.IgnoreExtraElements, new ConventionPack { new IgnoreExtraElementsConvention(true) }, _ => true);
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
        RegisterClassMaps();
    }

    static void RegisterClassMaps()
    {
        var classMaps = typeof(EventClassMap).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false })
            .Select(type => (Type: type, Interface: Array.Find(type.GetInterfaces(), _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IBsonClassMapFor<>))))
            .Where(_ => _.Interface is not null);

        foreach (var (type, @interface) in classMaps)
        {
            var documentType = @interface!.GetGenericArguments()[0];
            if (BsonClassMap.IsClassMapRegistered(documentType))
            {
                continue;
            }

            var classMap = (BsonClassMap)Activator.CreateInstance(typeof(BsonClassMap<>).MakeGenericType(documentType))!;
            @interface.GetMethod(nameof(IBsonClassMapFor<object>.Configure))!.Invoke(Activator.CreateInstance(type), [classMap]);
            BsonClassMap.RegisterClassMap(classMap);
        }
    }
}
