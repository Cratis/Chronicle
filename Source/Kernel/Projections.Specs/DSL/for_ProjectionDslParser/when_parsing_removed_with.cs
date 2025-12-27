// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_removed_with : Specification
{
    const string Dsl = @"Users
| name=UserRegistered.name
| removedWith UserDeleted";

    ProjectionDefinition _result;

    void Because() => _result = ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_have_removed_with_event() => _result.RemovedWith.Count.ShouldEqual(1);
}
