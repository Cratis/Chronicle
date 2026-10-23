// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_VerifyFencedMongoTopology;

public class when_the_resolved_destination_is_standalone : Specification
{
    Exception? _error;
    VerifyFencedMongoTopology _startup;

    void Establish()
    {
        var resolved = Substitute.For<IMongoDatabase>();
        resolved.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BsonDocument("isWritablePrimary", true)));
        var database = Substitute.For<IDatabase>();
        database.GetReadModelDatabase(EventStoreName.System, EventStoreNamespaceName.Default).Returns(resolved);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IDatabase)).Returns(database);
        var options = new ChronicleOptions { Features = new Features { ProjectionGenerations = true } };
        _startup = new VerifyFencedMongoTopology(Options.Create(options), services);
    }

    async Task Because() => _error = await Catch.Exception(() => _startup.Execute(CancellationToken.None));

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_how_to_enable_transactions() => _error!.Message.ShouldContain("--replSet");
}
