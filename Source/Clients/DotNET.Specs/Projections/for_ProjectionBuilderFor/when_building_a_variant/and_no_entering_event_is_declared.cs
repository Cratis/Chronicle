// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

/// <summary>
/// Every handler on a variant other than its entering event is update-only, so a variant with no entering event
/// declared would never be created and therefore never written to at all. That is reported rather than built.
/// </summary>
public class and_no_entering_event_is_declared : Specification
{
    ProjectionBuilderFor<BacklogItem> _builder;
    Exception _result;

    void Establish()
    {
        _builder = new ProjectionBuilderFor<BacklogItem>(
            new ProjectionId(typeof(BacklogItem).FullName),
            typeof(BacklogItem),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([typeof(IssueCreated)]),
            new JsonSerializerOptions());

        _builder.VariantOf<WorkItem>(_ => _.Id).From<IssueCreated>();
    }

    async Task Because() => _result = await Catch.Exception(() => Task.FromResult<ProjectionDefinition>(_builder.Build()));

    [Fact] void should_fail() => _result.ShouldNotBeNull();

    [Fact] void should_fail_with_the_missing_entering_event_reason() => _result.ShouldBeOfExactType<VariantMustDeclareEntersOnEvent>();
}
