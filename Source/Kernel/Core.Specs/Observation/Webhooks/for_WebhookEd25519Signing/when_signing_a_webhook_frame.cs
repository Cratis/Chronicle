// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

/// <summary>
/// Independent vector generated with Node crypto (Ed25519, PKCS8 DER seed prefix):
/// <code language="sh">
/// node -e 'const {createPrivateKey,sign}=require("node:crypto");const seed="9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60";const key=createPrivateKey({key:Buffer.from("302e020100300506032b657004220420"+seed,"hex"),format:"der",type:"pkcs8"});console.log(sign(null,Buffer.from("msg_p5jXN8AQM9LWM0D4loKWxJek.1614265330.{\"test\": 2432232314}"),key).toString("base64"));'
/// </code>
/// Public test material, not an operational key.
/// </summary>
public class when_signing_a_webhook_frame : given.an_ed25519_fixture
{
    string _signature;

    void Because() => _signature = WebhookEd25519Signing.Sign("msg_p5jXN8AQM9LWM0D4loKWxJek", 1614265330, "{\"test\": 2432232314}"u8, _test1Seed);

    [Fact] void should_match_the_independent_signature() => _signature.ShouldEqual("v1a,fldxM4gAKugP6nnt1hdz3sgGfZ6d99nzrMFnZOELIxbzEHoVmAb2ADpkJK7zgPePmPsle0zV9jSeGlHFG2NVAw==");
    [Fact] void should_encode_a_64_byte_signature() => Convert.FromBase64String(_signature[4..]).Length.ShouldEqual(64);
}
