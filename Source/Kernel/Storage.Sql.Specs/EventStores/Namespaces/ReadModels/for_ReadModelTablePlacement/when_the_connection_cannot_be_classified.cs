// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTablePlacement;

public class when_the_connection_cannot_be_classified : Specification
{
    ReadModelTablePlacement? _placement;

    void Because() => _placement = ReadModelTablePlacement.Of("DataSource=:memory:", "main");

    [Fact] void should_skip_placement_caching() => _placement.ShouldBeNull();
}
