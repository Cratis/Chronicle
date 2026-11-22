// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public class when_authorities_are_compared : Specification
{
    UniqueConstraintWriteAuthority _authority;
    UniqueConstraintWriteAuthority _equal;
    UniqueConstraintWriteAuthority[] _different;

    void Because()
    {
        var epoch = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var run = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _authority = new(new(epoch), UniqueConstraintWritePhase.Run(new(run)), new(3));
        _equal = new(new(epoch), UniqueConstraintWritePhase.Run(new(run)), new(3));
        _different =
        [
            _authority with { Epoch = new(run) },
            _authority with { Phase = UniqueConstraintWritePhase.Live },
            _authority with { Phase = UniqueConstraintWritePhase.Run(new(epoch)) },
            _authority with { Revision = new(4) }
        ];
    }

    [Fact] void should_compare_equal_values_structurally() => _authority.ShouldEqual(_equal);
    [Fact] void should_compare_every_epoch_phase_run_and_revision_field() => _different.All(_ => _ != _authority).ShouldBeTrue();
    [Fact] void should_represent_live_without_a_run() => UniqueConstraintWritePhase.Live.RunId.ShouldBeNull();
}
