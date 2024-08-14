// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

[CollectionDefinition(Name)]
public class GlobalCollection : ICollectionFixture<GlobalFixture>
{
    public const string Name = "Global";
}
