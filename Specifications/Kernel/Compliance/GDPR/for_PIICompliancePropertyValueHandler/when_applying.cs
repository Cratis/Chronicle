// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Aksio.Cratis.Kernel.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_applying : given.a_property_handler
{
    JsonNode input;
    JsonNode result;
    string encrypted_string = "Hello";
    byte[] encrypted_bytes;

    void Establish()
    {
        input = JsonValue.Create(42);
        encrypted_bytes = Encoding.UTF8.GetBytes(encrypted_string);
        encryption.Setup(_ => _.Encrypt(IsAny<byte[]>(), key)).Returns(encrypted_bytes);
    }

    async Task Because() => result = await handler.Apply(string.Empty, string.Empty, identifier, input);

    [Fact] void should_return_encrypted_string() => result.ToString().ShouldEqual(Convert.ToBase64String(encrypted_bytes));
}
