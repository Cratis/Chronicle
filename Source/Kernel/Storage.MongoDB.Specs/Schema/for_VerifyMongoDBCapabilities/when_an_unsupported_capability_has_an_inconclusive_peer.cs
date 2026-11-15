// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_an_unsupported_capability_has_an_inconclusive_peer : given.a_verification
{
    void Establish() => _report = new(MongoDBCapability.Unsupported("not supported yet"), MongoDBCapability.Inconclusive("timeout"));
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_fail_startup() => (_error is MongoDBCapabilitiesNotSupported).ShouldBeTrue();
    [Fact] void should_name_the_unsupported_capability() => _error!.Message.Contains("change streams", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_label_the_inconclusive_capability_as_unsupported() => _error!.Message.Contains("collation", StringComparison.Ordinal).ShouldBeFalse();
}
