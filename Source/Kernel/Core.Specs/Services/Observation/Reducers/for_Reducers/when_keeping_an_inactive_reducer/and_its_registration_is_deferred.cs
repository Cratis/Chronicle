// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_keeping_an_inactive_reducer;

/// <summary>
/// A registration a webhook still holds back is tried again every revise interval, not left to the safety net.
/// </summary>
public class and_its_registration_is_deferred : given.an_inactive_reducer
{
    void Establish() => _reducer.IsRegistrationDeferred().Returns(true);

    Task Because() => Keep();

    [Fact] async Task should_try_to_complete_the_registration() => await _reducer.Received(1).KeepSubscription(_definition);
    [Fact] void should_not_be_confirmed_intact() => _intact.ShouldBeFalse();
}
