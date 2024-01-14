// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Aksio.Cratis.Kernel.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_releasing : given.a_property_handler
{
    JsonNode input;
    JsonNode result;
    string decrypted_string = "Hello";
    byte[] decrypted_bytes;

    void Establish()
    {
        input = JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes("42")));
        decrypted_bytes = Encoding.UTF8.GetBytes(decrypted_string);
        encryption.Setup(_ => _.Decrypt(IsAny<byte[]>(), key)).Returns(decrypted_bytes);
    }

    async Task Because() => result = await handler.Release(string.Empty, string.Empty, identifier, input);

    [Fact] void should_return_encrypted_string() => result.ToString().ShouldEqual(decrypted_string);
}
