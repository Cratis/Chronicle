// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel;

[CollectionDefinition(Name)]
public class GlobalCollection : ICollectionFixture<GlobalFixture>
{
    public const string Name = "Global";
}
