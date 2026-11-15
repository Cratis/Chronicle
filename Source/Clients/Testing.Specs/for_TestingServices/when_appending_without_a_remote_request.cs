// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_appending_without_a_remote_request
{
    [Fact]
    public async Task should_append_through_the_real_in_process_services()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(TestEvent)]);
        using var store = new EventStoreForTesting(null, artifacts);
        ((IChronicleServicesAccessor)store.Connection).Services.ShouldBeOfExactType<TestingServices>();
        var result = await store.EventLog.Append("source", new TestEvent("in process"));
        result.IsSuccess.ShouldBeTrue();
    }
}
