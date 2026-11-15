// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

public class and_the_primary_holds_one : given.two_key_stores
{
    static readonly EncryptionKey _stored = KeyNamed("stored");
    static readonly EncryptionKey _probe = KeyNamed("probe");

    EncryptionKey _result;
    EncryptionKey _inSecondary;

    Task Establish() => _primary.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, _stored);

    async Task Because()
    {
        _result = await _composite.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, KeyNamed("candidate"));
        _inSecondary = await _secondary.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, _probe);
    }

    [Fact] void should_return_the_primary_secret() => _result.Private.ShouldEqual(_stored.Private);
    [Fact] void should_not_write_the_secret_to_the_secondary() => _inSecondary.Private.ShouldEqual(_probe.Private);
}
