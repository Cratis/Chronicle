// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionsRegistrationFailed;

public class when_the_failures_are_wrapped : Specification
{
    static readonly Engine.ProjectionDefinitionRegistrationFailed _definitionFailure = new(
        "the-failing-projection",
        new InvalidOperationException("the root cause"));
    static readonly Engine.ProjectionDefinitionRegistrationFailed _otherDefinitionFailure = new(
        "the-other-failing-projection",
        new InvalidOperationException("the other root cause"));
    static readonly Engine.ProjectionDefinitionsRegistrationFailed _failures = new(
        new Dictionary<ProjectionId, Engine.ProjectionDefinitionRegistrationFailed>
        {
            [_definitionFailure.Identifier] = _definitionFailure
        });
    static readonly Engine.ProjectionDefinitionsRegistrationFailed _otherFailures = new(
        new Dictionary<ProjectionId, Engine.ProjectionDefinitionRegistrationFailed>
        {
            [_otherDefinitionFailure.Identifier] = _otherDefinitionFailure
        });

    [Fact] void should_merge_them_from_every_aggregate_branch() => Find(new AggregateException(_failures, _otherFailures)).Keys.ShouldContainOnly(_definitionFailure.Identifier, _otherDefinitionFailure.Identifier);
    [Fact] void should_find_them_under_an_inner_exception_chain() => Find(new InvalidOperationException("outer", new InvalidOperationException("middle", _failures))).Keys.ShouldContainOnly(_definitionFailure.Identifier);
    [Fact] void should_not_treat_a_mixed_aggregate_as_fully_attributed() => Engine.ProjectionDefinitionsRegistrationFailed.TryFindFailures(new AggregateException(new InvalidOperationException("unrelated"), _failures), out _).ShouldBeFalse();
    [Fact] void should_not_find_them_when_they_are_not_there() => Engine.ProjectionDefinitionsRegistrationFailed.TryFindFailures(new InvalidOperationException("unrelated"), out _).ShouldBeFalse();
    [Fact] void should_not_find_them_in_nothing() => Engine.ProjectionDefinitionsRegistrationFailed.TryFindFailures(null, out _).ShouldBeFalse();

    static IReadOnlyDictionary<ProjectionId, Engine.ProjectionDefinitionRegistrationFailed> Find(Exception exception)
    {
        Engine.ProjectionDefinitionsRegistrationFailed.TryFindFailures(exception, out var failures).ShouldBeTrue();
        return failures!;
    }
}
