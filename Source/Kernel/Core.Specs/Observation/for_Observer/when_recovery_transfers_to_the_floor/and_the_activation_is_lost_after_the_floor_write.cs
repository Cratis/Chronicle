// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recovery_transfers_to_the_floor;

public class and_the_activation_is_lost_after_the_floor_write : and_the_activation_is_lost_before_the_floor_write
{
    protected override int CrashPoint => 1;
}
