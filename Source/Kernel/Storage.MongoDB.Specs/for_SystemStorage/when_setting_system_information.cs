// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage;

public class when_setting_system_information : given.a_system_storage
{
    SystemInformation _systemInformation;

    void Establish() => _systemInformation = new SystemInformation(new SemanticVersion(2, 3, 4));

    async Task Because() => await _storage.SetSystemInformation(_systemInformation);

    [Fact] void should_replace_document_in_collection() => _collection.Received(1).ReplaceOneAsync(
        Arg.Any<FilterDefinition<MongoDBSystemInformation>>(),
        Arg.Is<MongoDBSystemInformation>(doc => doc.Id == 0 && doc.Version.Equals(new SemanticVersion(2, 3, 4))),
        Arg.Is<ReplaceOptions>(opts => opts.IsUpsert),
        Arg.Any<CancellationToken>());
}
