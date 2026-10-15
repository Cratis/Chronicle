// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_read_enrolled_explicit_only : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        EnrollReadScope();
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_not_append() => _request.ShouldBeNull();
    [Fact] void should_not_validate_read_only_conditions() => _unit.GetAppendConditions().ShouldBeEmpty();
}
