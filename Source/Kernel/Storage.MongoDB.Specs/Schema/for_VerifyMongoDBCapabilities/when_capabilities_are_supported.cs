// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_capabilities_are_supported : given.a_verification
{
    Task Because() => _task.Execute(CancellationToken.None);

    [Fact] void should_probe_once() => _inspections.ShouldEqual(1);
    [Fact] void should_not_log() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log").ShouldBeFalse();
}
