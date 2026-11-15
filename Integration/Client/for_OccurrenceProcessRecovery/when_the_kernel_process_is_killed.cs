// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public class when_the_kernel_process_is_killed : Cratis.Specifications.Specification
{
    [Fact]
    public async Task should_fence_the_live_holder_and_recover_one_winning_effect_after_restart()
    {
        await using var scenario = await RecoveryScenario.Start();
        await scenario.KillKernelAndRecover();
    }
}
