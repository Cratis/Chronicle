// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

/// <summary>Shares the standard fixture; each scenario still owns its namespace and backend state.</summary>
[CollectionDefinition(Name)]
public class NamespaceFenceCollection : ICollectionFixture<NamespaceFenceFixture>
{
    /// <summary>The collection name.</summary>
    public const string Name = "NamespaceFence";
}
