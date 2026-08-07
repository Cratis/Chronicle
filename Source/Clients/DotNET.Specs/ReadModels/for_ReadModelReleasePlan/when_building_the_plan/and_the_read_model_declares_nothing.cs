// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// The shape every read model that existed before per-property declarations has. It must produce an empty
/// plan, because an empty plan is what routes the release through the single-subject path unchanged.
/// </summary>
public class and_the_read_model_declares_nothing : Specification
{
    record Employee(string Id, [PII] string Name);

    ReadModelReleasePlan _result;

    void Because() => _result = ReadModelReleasePlan.For(typeof(Employee));

    [Fact] void should_not_have_declarations() => _result.HasDeclarations.ShouldBeFalse();
    [Fact] void should_have_no_groups() => _result.Groups.ShouldBeEmpty();
}
