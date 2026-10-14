// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_only_a_protected_read : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        Read();
        await Commit();
    });

    [Fact] void should_complete_the_owner_protected_decision() => _error.ShouldBeNull();
    [Fact] void should_skip_the_validate_only_append_under_enforcement() => _request.ShouldBeNull();
    [Fact] void should_have_no_sent_conditions() => _unit.GetAppendConditions().ShouldBeEmpty();
}
