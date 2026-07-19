// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Collection fixture that shares a single <see cref="DnsServerFixture"/> across all chronicle+srv integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class DnsServerCollection : ICollectionFixture<DnsServerFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "DNS Server";
}
