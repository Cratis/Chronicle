// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor;

[CollectionDefinition(Name, DisableParallelization = true)]
public class SupervisorCollection : ICollectionFixture<SupervisorFixture>
{
    public const string Name = "Silo supervisor crash integration";
}
