// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_the_value_decrypts : given.a_property_handler
{
    JsonNode _value;
    ReleasedProtectedValue _result;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns(Encoding.UTF8.GetBytes("original value"));
    }

    async Task Because() => _result = await _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_report_decrypted() => _result.Status.ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_recover_the_original_value() => _result.Value.GetValue<string>().ShouldEqual("original value");
}
