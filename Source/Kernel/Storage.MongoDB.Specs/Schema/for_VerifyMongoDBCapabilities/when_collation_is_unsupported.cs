// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_collation_is_unsupported : given.a_verification
{
    void Establish() => _report = _report with { Collation = MongoDBCapability.Unsupported("not implemented") };
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_fail_startup() => (_error is MongoDBCapabilitiesNotSupported).ShouldBeTrue();
    [Fact] void should_name_collation() => _error!.Message.Contains("collation", StringComparison.Ordinal).ShouldBeTrue();
}
