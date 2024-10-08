// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Base;

[CollectionDefinition(Name)]
public class GlobalCollection : ICollectionFixture<GlobalFixture>
{
    public const string Name = "Global";
}
