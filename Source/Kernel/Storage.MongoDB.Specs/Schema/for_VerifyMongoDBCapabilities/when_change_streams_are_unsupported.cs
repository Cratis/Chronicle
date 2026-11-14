// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_change_streams_are_unsupported : given.a_verification
{
    void Establish() => _report = _report with { ChangeStreams = MongoDBCapability.Unsupported("mongodb://user:secret@private-host") };
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_fail_startup() => (_error is MongoDBCapabilitiesNotSupported).ShouldBeTrue();
    [Fact] void should_be_an_invalid_operation() => (_error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_name_change_streams() => _error!.Message.Contains("change streams", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_the_requirement() => _error!.Message.Contains("does not support the capabilities Chronicle requires", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_link_to_compatibility_documentation() => _error!.Message.Contains("/chronicle/hosting/storage-compatibility/", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_include_connection_details() => _error!.Message.Contains("secret", StringComparison.Ordinal).ShouldBeFalse();
}
