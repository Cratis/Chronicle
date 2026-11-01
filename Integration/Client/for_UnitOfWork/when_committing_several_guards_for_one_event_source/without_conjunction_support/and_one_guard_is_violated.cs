// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.without_conjunction_support.and_one_guard_is_violated.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.without_conjunction_support;

[Collection(ChronicleCollection.Name)]
public class and_one_guard_is_violated(context context) : when_committing_several_guards_for_one_event_source.and_one_guard_is_violated<context>(context)
{
    public class context(ChronicleFixture fixture) : when_committing_several_guards_for_one_event_source.and_one_guard_is_violated.context(fixture)
    {
        public override bool KernelSupportsConjunctions => false;
    }

    [Fact] void should_take_the_fallback_route() => Context.AdvertisedConjunctions.ShouldBeFalse();
    [Fact] void should_refuse_the_unsupported_conjunction() => Context.Error.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_identify_the_source() => ((IncompatibleConcurrencyScopesForEventSource)Context.Error).EventSourceId.ShouldEqual(Context.Source);
    [Fact] void should_identify_both_guards() => ((IncompatibleConcurrencyScopesForEventSource)Context.Error).Scopes.Count.ShouldEqual(2);
}
