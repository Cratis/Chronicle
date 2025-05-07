// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

/// <summary>
/// Collection fixture for the Chronicle integration tests.
/// </summary>
[CollectionDefinition(Name)]
public class ChronicleCollection : ICollectionFixture<ChronicleFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Chronicle";
}
