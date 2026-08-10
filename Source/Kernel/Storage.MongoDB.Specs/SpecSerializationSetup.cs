// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Registers the same MongoDB serialization the real server uses, once, before any spec runs.
/// </summary>
/// <remarks>
/// The setup is invoked by <see cref="MongoDBTestFramework"/>. At that point all module initializers in the spec
/// assembly have completed, including the generated type-discovery provider registration. Calling Arc's public
/// composition APIs can therefore initialize the process-wide type universe, derived types, conventions, providers,
/// and class maps from the complete generated discovery graph rather than freezing a hand-built subset.
/// <para>
/// The same holds for <see cref="ConstraintDefinitionSerializationProvider"/>, which is what decides whether a
/// constraint definition persisted by an older kernel is upgraded on read. It is Chronicle-specific and therefore
/// remains registered in addition to Arc's canonical MongoDB initialization.
/// </para>
/// </remarks>
internal static class SpecSerializationSetup
{
    static readonly Lazy<bool> _initialization = new(InitializeCore);

    /// <summary>
    /// Registers the server's MongoDB serialization conventions, providers and class maps.
    /// </summary>
    internal static void Initialize() => _ = _initialization.Value;

    static bool InitializeCore()
    {
        var services = new ServiceCollection();
        services.AddCratisArcCore();
        services.AddCratisMongoDB(configureMongoDB: mongoDB => mongoDB.WithCamelCaseNamingPolicy());
        BsonSerializer.RegisterSerializationProvider(new ConstraintDefinitionSerializationProvider());
        return true;
    }
}
