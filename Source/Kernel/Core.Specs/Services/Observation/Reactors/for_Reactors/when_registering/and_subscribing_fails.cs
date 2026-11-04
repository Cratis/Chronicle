// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_registering;

public class and_subscribing_fails : given.a_registration
{
    void Establish() => _client.SetDefinitionAndSubscribe(Arg.Any<ReactorDefinition>()).Returns(Task.FromException(new InvalidOperationException("Registration failed")));

    async Task Because()
    {
        Connect();
        await _error.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_release_the_lease() => _lease.Received(1).Dispose();
}
