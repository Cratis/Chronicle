// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_there_is_no_second_held_partition : and_live_delivery_settles_after_another_applicable_prefix_event
{
    protected override bool HoldT => false;
}
