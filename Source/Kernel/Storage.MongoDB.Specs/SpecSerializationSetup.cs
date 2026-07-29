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
/// The same reasoning applies to <see cref="EventClassMap"/>, which is what makes the event's sequence number the
/// document <c>_id</c>. Any spec that merely renders a <c>Builders&lt;Event&gt;</c> expression — building an index
/// key, for instance — makes the driver auto-register a default class map for <see cref="Event"/>, and a default
/// map gives every appended event a generated <c>ObjectId</c> instead. Registering it lazily from a spec base
/// therefore loses a race against unrelated spec classes running in parallel, silently dropping both the tail
/// aggregation and the duplicate-sequence-number detection that depend on that mapping.
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
        BsonClassMap.RegisterClassMap<Event>(classMap => new EventClassMap().Configure(classMap));
    }
}
