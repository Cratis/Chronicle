// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Registers the same MongoDB serialization the real server uses, once, before any spec runs.
/// </summary>
/// <remarks>
/// Runs at assembly load (before any <see cref="BsonClassMap"/> is frozen) so document serialization in specs
/// is faithful to the silo: the camelCase element-name convention (the persisted field is "status", not
/// "Status") and concept serialization. Doing this in a spec base static constructor is too late once another
/// spec has already touched a type — a fidelity gap that let a serializer-dependent index bug slip past specs.
/// </remarks>
internal static class SpecSerializationSetup
{
    /// <summary>
    /// Registers the server's MongoDB serialization conventions and providers.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        new ConventionPacks().Provide();
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
    }
}
