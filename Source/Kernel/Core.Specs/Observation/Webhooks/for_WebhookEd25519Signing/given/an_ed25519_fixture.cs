// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing.given;

/// <summary>
/// RFC 8032 section 7.1, TEST 1 and TEST 3, copied from
/// <see href="https://www.rfc-editor.org/rfc/rfc8032#section-7.1"/>.
/// Public test material, not an operational key.
/// </summary>
public class an_ed25519_fixture : Specification
{
    protected byte[] _test1Seed;
    protected byte[] _test1PublicKey;
    protected byte[] _test1Signature;
    protected byte[] _test3Seed;
    protected byte[] _test3PublicKey;
    protected byte[] _test3Message;
    protected byte[] _test3Signature;

    void Establish()
    {
        _test1Seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
        _test1PublicKey = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
        _test1Signature = Convert.FromHexString("e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b");
        _test3Seed = Convert.FromHexString("c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7");
        _test3PublicKey = Convert.FromHexString("fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025");
        _test3Message = Convert.FromHexString("af82");
        _test3Signature = Convert.FromHexString("6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40a");
    }
}
