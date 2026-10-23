// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;

public static class duplicate_keys
{
    public static MongoWriteException Duplicate()
    {
        // The driver exposes these values on exceptions but keeps their constructors internal.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var write = (WriteError)Activator.CreateInstance(typeof(WriteError), flags, null, [ServerErrorCategory.DuplicateKey, 11000, "duplicate", new BsonDocument()], null)!;
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new(connection, write, null, null);
    }

    public static IAsyncCursor<T> CursorOf<T>(params T[] documents)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.MoveNextAsync(default).ReturnsForAnyArgs(true, false);
        cursor.Current.Returns(documents);
        return cursor;
    }
}
