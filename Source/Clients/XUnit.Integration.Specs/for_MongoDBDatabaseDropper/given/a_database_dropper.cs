// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.given;

public class a_database_dropper : Specification
{
    protected const string DatabaseName = "testing+es+Default";
    protected IMongoClient _client;
    protected IMongoDatabase _database;
    protected TimeSpan _elapsed;
    protected TimeSpan _timeout = TimeSpan.FromMilliseconds(200);
    protected List<TimeSpan> _delays = [];
    protected List<string> _commands = [];
    protected List<string> _createdProbes = [];
    protected List<string> _removedProbes = [];
    protected Func<Task> _drop = () => Task.CompletedTask;
    protected Func<Task> _create = () => Task.CompletedTask;
    protected Func<Task> _remove = () => Task.CompletedTask;
    protected Exception _error;
    protected int _drops;
    protected int _creates;
    protected int _removes;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _database = Substitute.For<IMongoDatabase>();
        _client.GetDatabase(DatabaseName, Arg.Any<MongoDatabaseSettings>()).Returns(_database);
        _client.DropDatabaseAsync(DatabaseName, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _drops++;
            _commands.Add("drop");
            return _drop();
        });
        _database.CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _creates++;
            _commands.Add("create");
            _createdProbes.Add(call.ArgAt<string>(0));
            return _create();
        });
        _database.DropCollectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _removes++;
            _commands.Add("remove");
            _removedProbes.Add(call.ArgAt<string>(0));
            return _remove();
        });
    }

    protected Task Delay(TimeSpan duration)
    {
        _delays.Add(duration);
        _elapsed += duration;
        return Task.CompletedTask;
    }

    protected static MongoCommandException CommandFailure(int code, string message = "opaque command failure") => new(
        new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
        message,
        new BsonDocument("create", "probe"),
        new BsonDocument { { "ok", 0 }, { "code", code }, { "errmsg", message } });
}
