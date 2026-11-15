// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_capabilities_are_inconclusive : given.a_verification
{
    void Establish() => _report = new(MongoDBCapability.Inconclusive("authorization failed"), MongoDBCapability.Inconclusive("network unavailable"));
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_continue_startup() => _error.ShouldBeNull();
    [Fact] void should_warn() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
}
