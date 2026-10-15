// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_an_alternate_unit_receives_options;

public class without_named_tags : Specification
{
    readonly given.a_legacy_unit _unit = new();
    readonly ConcurrencyScope _scope = new(7);
    void Because() => ((IUnitOfWork)_unit).AddEventWithOptions(EventSequenceId.Log, "source", "event", Causation.Unknown(), new(EventStreamType: "stream", ConcurrencyScope: _scope, Origin: ConcurrencyScopeOrigin.Aggregate, Tags: ["tag"]));
    [Fact] void should_use_the_legacy_member() => _unit.NamedTagsUsed.ShouldBeFalse();
    [Fact] void should_preserve_the_scope() => _unit.Scope.ShouldBeSame(_scope);
    [Fact] void should_preserve_the_options() => _unit.Event!.EventStreamType.Value.ShouldEqual("stream");
    [Fact] void should_preserve_tags() => _unit.Event!.Tags.ShouldContainOnly("tag");
}
