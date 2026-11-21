// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_the_fingerprint_is_recorded_as_erased : given.a_signed_value_after_erasure
{
    JsonNode _result;

    void Establish() => _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(FenceRecording(_key.Fingerprint));

    async Task Because() => _result = await _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_release_an_empty_value() => _result.ToString().ShouldEqual(string.Empty);
}
