// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryRevision.when_taking_the_next_revision;

public class and_it_is_not_at_the_maximum : Specification
{
    DeliveryRevision _result;

    void Because() => _result = DeliveryRevision.None.Next();

    [Fact] void should_advance_to_the_first_stored_revision() => _result.Value.ShouldEqual(1UL);
}
