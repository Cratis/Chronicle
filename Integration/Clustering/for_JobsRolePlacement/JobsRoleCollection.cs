// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

[CollectionDefinition(Name, DisableParallelization = true)]
public class JobsRoleCollection : ICollectionFixture<JobsRoleFixture>
{
    public const string Name = "Chronicle Jobs role placement";
}
