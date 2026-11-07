// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_a_legitimate_value_is_empty : given.a_property_handler
{
    JsonNode _value;
    ReleasedProtectedValue _result;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns([]);
    }

    async Task Because() => _result = await _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_report_decrypted_rather_than_erased() => _result.Status.ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_preserve_the_legitimate_empty_string() => _result.Value.GetValue<string>().ShouldEqual(string.Empty);
}
