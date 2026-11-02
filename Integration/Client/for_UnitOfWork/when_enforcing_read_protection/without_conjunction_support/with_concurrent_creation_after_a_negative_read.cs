// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support.with_concurrent_creation_after_a_negative_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support;

[Collection(ChronicleCollection.Name)]
public class with_concurrent_creation_after_a_negative_read(context context) : when_enforcing_read_protection.with_concurrent_creation_after_a_negative_read<context>(context)
{
    public class context(ChronicleFixture fixture) : when_enforcing_read_protection.with_concurrent_creation_after_a_negative_read.context(fixture)
    {
        public override bool KernelSupportsConjunctions => false;
    }

    [Fact] void should_take_the_fallback_route() => Context.AdvertisedConjunctions.ShouldBeFalse();
    [Fact] void should_keep_the_negative_condition() => Context.Unit.GetAppendConditions().Single().ExpectsNoMatchingEvent.ShouldBeTrue();
}
