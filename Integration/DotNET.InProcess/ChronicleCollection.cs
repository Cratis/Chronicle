// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration;

[CollectionDefinition(Name)]
public class ChronicleCollection : ICollectionFixture<ChronicleInProcessFixture>
{
    public const string Name = "Chronicle";
}
