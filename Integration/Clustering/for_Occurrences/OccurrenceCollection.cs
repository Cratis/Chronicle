// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[CollectionDefinition(Name, DisableParallelization = true)]
public class OccurrenceCollection : ICollectionFixture<OccurrenceFixture>
{
    public const string Name = "MongoDB occurrence crash integration";
}
