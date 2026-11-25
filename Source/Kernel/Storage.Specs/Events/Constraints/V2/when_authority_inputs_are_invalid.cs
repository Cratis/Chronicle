// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public class when_authority_inputs_are_invalid : Specification
{
    Exception _epoch;
    Exception _run;
    Exception _revision;

    void Because()
    {
        _epoch = Catch.Exception(() => _ = new UniqueConstraintEpoch(Guid.Empty));
        _run = Catch.Exception(() => _ = new UniqueConstraintRunId(Guid.Empty));
        _revision = Catch.Exception(() => _ = new UniqueConstraintAuthorityRevision(-1));
    }

    [Fact] void should_reject_an_empty_epoch() => _epoch.ShouldBeOfExactType<InvalidUniqueConstraintV2Input>();
    [Fact] void should_reject_an_empty_run() => _run.ShouldBeOfExactType<InvalidUniqueConstraintV2Input>();
    [Fact] void should_reject_a_negative_revision() => _revision.ShouldBeOfExactType<InvalidUniqueConstraintV2Input>();
}
