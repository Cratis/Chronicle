// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_the_activation_restarts_after_the_last_owner_ack : and_live_arrives_between_the_last_repair_ack_and_the_successor
{
    protected override bool DiscardActivation => true;
}
