// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_registering;

public class and_reading_the_definition_fails : given.a_registration
{
    void Establish() => _readModel.GetDefinition().Returns(Task.FromException<ReadModelDefinition>(new InvalidOperationException("Definition unavailable")));

    async Task Because()
    {
        Connect();
        await _error.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_release_the_lease() => _lease.Received(1).Dispose();
    [Fact] void should_not_start_a_reducer_subscription() => _client.ReceivedCalls().ShouldBeEmpty();
}
