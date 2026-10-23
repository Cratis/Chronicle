// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[CollectionDefinition(Name)]
public class ReplayPlacementCollection : ICollectionFixture<ReplayPlacementFixture>
{
    public const string Name = "replay-placement";
}
