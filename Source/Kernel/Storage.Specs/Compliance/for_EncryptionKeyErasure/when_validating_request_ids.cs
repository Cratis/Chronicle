// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_validating_request_ids
{
    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void should_reject_blank_ids(string id) => Assert.Throws<ArgumentException>(() => EncryptionKeyErasureRequestIdentity.Validate(id));

    [Fact] public void should_reject_more_than_128_characters() => Assert.Throws<ArgumentException>(() => EncryptionKeyErasureRequestIdentity.Validate(new string('x', 129)));
    [Fact] public void should_accept_128_opaque_characters() => EncryptionKeyErasureRequestIdentity.Validate(new string('?', 128));
    [Fact] public void should_not_parse_or_normalize_an_opaque_identity() => EncryptionKeyErasureRequestIdentity.Validate("  not/a-uuid ?  ");
}
