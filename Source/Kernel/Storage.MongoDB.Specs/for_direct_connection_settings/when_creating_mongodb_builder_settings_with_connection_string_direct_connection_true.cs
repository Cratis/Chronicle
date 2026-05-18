// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Setup;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_direct_connection_settings;

public class when_creating_mongodb_builder_settings_with_connection_string_direct_connection_true : Specification
{
    MongoClientSettings _settings = default!;

    void Because()
    {
        var method = typeof(MongoDBChronicleBuilderExtensions).GetMethod("GetMongoClientSettings", BindingFlags.NonPublic | BindingFlags.Static);
        _settings = method!.Invoke(null, ["mongodb://localhost:27017/?directConnection=true"]) as MongoClientSettings ?? throw new InvalidOperationException("Expected MongoClientSettings from GetMongoClientSettings");
    }

    [Fact] void should_keep_direct_connection_enabled() => _settings.DirectConnection.ShouldBeTrue();
}
