// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning.given;

public class a_signing_fixture : Specification
{
    protected byte[] _secret;
    protected byte[] _payload;

    void Establish()
    {
        // Public test material: bytes 0..31, not an operational key.
        _secret = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");
        _payload = "{\"ok\":true}"u8.ToArray();
    }
}
