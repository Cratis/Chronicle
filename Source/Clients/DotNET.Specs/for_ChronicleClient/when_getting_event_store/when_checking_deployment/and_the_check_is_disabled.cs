// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_the_check_is_disabled : given.a_serverless_client
{
    void Establish()
    {
        _options.Serverless.CheckArtifactFingerprint = false;
        CreateClient();
    }

    Task Because() => _client.GetEventStore("disabled", "one");

    [Fact] void should_not_schedule_any_task() => ((ICollection)typeof(ChronicleClient).GetField("_deploymentChecks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client)!).Count.ShouldEqual(0);
    [Fact] void should_not_allocate_a_diagnostic_cancellation_source() => typeof(ChronicleClient).GetField("_deploymentCheckCancellation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client).ShouldBeNull();
    [Fact] void should_not_query_metadata() => _services.EventStores.DidNotReceiveWithAnyArgs().GetEventStoreDeployment(default!, default);
}
