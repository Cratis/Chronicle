// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_an_alternate_unit_receives_options;

public class with_named_tags : Specification
{
    readonly given.a_legacy_unit _unit = new();
    readonly NamedTag _tag = new("name", "value");
    void Because() => ((IUnitOfWork)_unit).AddEventWithOptions(EventSequenceId.Log, "source", "event", Causation.Unknown(), new(Origin: ConcurrencyScopeOrigin.Routing, NamedTags: [_tag]));
    [Fact] void should_use_the_named_tag_member() => _unit.NamedTagsUsed.ShouldBeTrue();
    [Fact] void should_preserve_the_named_tags() => _unit.Event!.NamedTags.ShouldContainOnly(_tag);
}
