// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public class when_the_application_process_is_killed : Cratis.Specifications.Specification
{
    [Fact]
    public async Task should_recover_the_committed_receipt_without_a_second_effect()
    {
        await using var scenario = await RecoveryScenario.Start();
        await scenario.KillApplicationAndRecover();
    }
}
