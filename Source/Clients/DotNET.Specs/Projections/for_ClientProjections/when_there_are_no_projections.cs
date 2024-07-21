// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ClientProjections;

public class when_there_are_no_projections : given.client_projections_without_any_definitions
{
    IEnumerable<ProjectionDefinition> all;

    void Because() => all = [.. definitions.Definitions];

    [Fact] void should_return_empty_list() => all.ShouldBeEmpty();
}
