// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_direct_connection_settings.given;

public class mongodb_client_manager_context : Specification
{
    protected IMongoDBClientManager _clientManager = default!;
    protected IOptions<MongoDBOptions> _mongoDBOptions = default!;
    protected ICustomSerializers _customSerializers = default!;
    protected MongoClientSettings _settings = default!;

    void Establish()
    {
        _clientManager = Substitute.For<IMongoDBClientManager>();
        var client = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings?>()).Returns(database);
        _clientManager.GetClientFor(Arg.Do<MongoClientSettings>(settings => _settings = settings)).Returns(client);

        _mongoDBOptions = Options.Create(new MongoDBOptions
        {
            Server = "mongodb://localhost:27017/?directConnection=true",
            Database = "chronicle"
        });
        _customSerializers = Substitute.For<ICustomSerializers>();
    }
}
