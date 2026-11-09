// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe.given;

public static class command_errors
{
    public static MongoCommandException Create(int code, string message) => new(
        new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
        message,
        new BsonDocument("aggregate", 1),
        new BsonDocument { { "ok", 0 }, { "code", code }, { "errmsg", message } });
}
