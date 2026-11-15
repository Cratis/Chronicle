// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_the_value_is_plain : given.a_property_handler
{
    JsonNode _value;
    ReleasedProtectedValue _result;

    void Establish() => _value = JsonValue.Create("plain value");

    async Task Because() => _result = await _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_report_plain() => _result.Status.ShouldEqual(ProtectedValueReleaseStatus.Plain);
    [Fact] void should_preserve_the_value() => _result.Value.ShouldBeSame(_value);
    [Fact] void should_not_look_up_a_key() => _keyStore.DidNotReceive().TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>());
}
