// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

public class and_the_primary_cannot_hold_internal_secrets : given.two_key_stores
{
    static readonly EncryptionKey _probe = KeyNamed("probe");

    IEncryptionKeyStorage _unsupported;
    Exception _error;
    EncryptionKey _inSecondary;

    void Establish()
    {
        _unsupported = Substitute.For<IEncryptionKeyStorage>();
        _unsupported.GetOrAddInternalSecretFor(Arg.Any<EventStoreName>(), Arg.Any<InternalSecretPurpose>(), Arg.Any<EncryptionKey>())
            .ThrowsAsync(new InternalSecretsNotSupported(InternalSecretPurpose.MutationReceiptDigest));
        _composite = new CompositeEncryptionKeyStorage(_unsupported, _secondary);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _composite.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, KeyNamed("candidate")));
        _inSecondary = await _secondary.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, _probe);
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<InternalSecretsNotSupported>();
    [Fact] void should_not_fall_back_to_the_secondary() => _inSecondary.Private.ShouldEqual(_probe.Private);
}
