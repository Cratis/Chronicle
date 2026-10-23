// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[CollectionDefinition(Name, DisableParallelization = true)]
public class DefaultRolesCollection : ICollectionFixture<DefaultRolesFixture>
{
    public const string Name = "Chronicle default role placement";
}
