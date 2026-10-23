// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework(
    "Cratis.Chronicle.Storage.MongoDB.MongoDBTestFramework",
    "Cratis.Chronicle.Storage.MongoDB.Specs")]

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// xUnit test framework that initializes the server-faithful MongoDB serialization setup before discovering or
/// executing specs.
/// </summary>
/// <param name="messageSink">The diagnostic message sink.</param>
public sealed class MongoDBTestFramework(IMessageSink messageSink)
    : XunitTestFramework(InitializeSerialization(messageSink))
{
    static IMessageSink InitializeSerialization(IMessageSink messageSink)
    {
        SpecSerializationSetup.Initialize();
        return messageSink;
    }
}
