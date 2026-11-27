// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_delivery_settles_after_another_applicable_prefix_event : and_live_delivery_settles_before_the_conclusion_enters
{
    protected override bool IncludeApplicableEventAtFourteen => true;

    protected override EventSequenceNumber[] ExpectedDeliveries() => [11UL, 14UL, 15UL, 16UL, 17UL];
}
