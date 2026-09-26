// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Specs;

/// <summary>
/// Helpers for creating the package's MongoDB jobs storage in specs.
/// </summary>
public static class JobsStorageForSpecs
{
    /// <summary>
    /// Create a <see cref="MongoDBJobsStorage"/> against the given client - never contacted unless storage is used.
    /// </summary>
    /// <param name="client">The client to use.</param>
    /// <returns>A <see cref="MongoDBJobsStorage"/> instance.</returns>
    public static MongoDBJobsStorage Create(IMongoClient client) =>
        new(
            client,
            Substitute.For<IJobTypes>(),
            Substitute.For<ICustomSerializers>(),
            Options.Create(new MongoDBJobsStorageOptions()));
}
