// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

[assembly: TestFramework(typeof(Cratis.Chronicle.Storage.MongoDB.MongoDBTestFramework))]

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Shared-lifecycle specification test framework that initializes the server-faithful MongoDB serialization setup
/// before discovering or executing specs.
/// </summary>
public sealed class MongoDBTestFramework : SpecificationTestFramework
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBTestFramework"/> class.
    /// </summary>
    public MongoDBTestFramework()
    {
        SpecSerializationSetup.Initialize();
    }
}
