// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_keeping_an_inactive_reducer;

/// <summary>
/// A silo of an older version does not know the question during a rolling deployment; the registration is then kept
/// every revise interval, the way it was before.
/// </summary>
public class and_the_silo_does_not_know_whether_it_is_deferred : given.an_inactive_reducer
{
    void Establish() => _reducer.IsRegistrationDeferred().Returns(Task.FromException<bool>(new NotSupportedException("An older silo")));

    Task Because() => Keep();

    [Fact] void should_not_be_confirmed_intact() => _intact.ShouldBeFalse();
}
