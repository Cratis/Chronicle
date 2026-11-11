// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_keeping_an_inactive_reducer;

public class and_its_registration_is_complete : given.an_inactive_reducer
{
    void Establish() => _reducer.IsRegistrationDeferred().Returns(false);

    Task Because() => Keep();

    [Fact] async Task should_keep_the_registration() => await _reducer.Received(1).KeepSubscription(_definition);
    [Fact] void should_be_confirmed_intact() => _intact.ShouldBeTrue();
}
