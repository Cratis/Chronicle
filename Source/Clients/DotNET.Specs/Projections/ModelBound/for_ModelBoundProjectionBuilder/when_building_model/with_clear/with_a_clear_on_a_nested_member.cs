// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// Clearing one member of a nested object is not the same as clearing the object. The member's clear lands on the
/// nested definition's own mappings, leaving the object itself standing.
/// </summary>
public class with_a_clear_on_a_nested_member : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ContractSigned), typeof(ContractNoticeWithdrawn)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(EmployeeSheet));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_nested_notice_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ContractNoticeWithdrawn)).ToContract();
        var nestedDef = _result.Nested[nameof(EmployeeSheet.Contract)];
        var fromDef = nestedDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties[nameof(EmployeeContract.NoticeGiven)].ShouldEqual(WellKnownExpressions.Null);
    }

    [Fact]
    void should_not_clear_the_whole_nested_object()
    {
        var nestedDef = _result.Nested[nameof(EmployeeSheet.Contract)];
        nestedDef.RemovedWith.ShouldBeEmpty();
    }
}

[EventType]
public record ContractSigned(string Title, string NoticeGiven);

[EventType]
public record ContractNoticeWithdrawn;

[FromEvent<ContractSigned>]
public record EmployeeContract(
    string Title,
    [ClearWith<ContractNoticeWithdrawn>] string? NoticeGiven);

public record EmployeeSheet(
    string EmployeeName,
    [Nested] EmployeeContract? Contract);
