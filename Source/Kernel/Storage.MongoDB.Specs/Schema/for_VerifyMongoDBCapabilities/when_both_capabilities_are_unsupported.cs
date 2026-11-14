// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_both_capabilities_are_unsupported : given.a_verification
{
    void Establish() => _report = new(MongoDBCapability.Unsupported("not implemented"), MongoDBCapability.Unsupported("not implemented"));
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_name_change_streams() => _error!.Message.Contains("change streams", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_collation() => _error!.Message.Contains("collation", StringComparison.Ordinal).ShouldBeTrue();
}
