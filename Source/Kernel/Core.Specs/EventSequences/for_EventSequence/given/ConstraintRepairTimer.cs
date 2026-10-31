// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public static class ConstraintRepairTimer
{
    public static Task Tick(TestKitSilo silo) => silo.TimerRegistry.FireAllAsync();
}
