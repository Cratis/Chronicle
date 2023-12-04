// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Common.Grains;

[CollectionDefinition(Name)]
public class OrleansClusterCollection : ICollectionFixture<OrleansClusterFixture>
{
    public const string Name = "Orleans cluster collection";
}
