// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EmulatorCertificateValidation.when_validating_a_certificate;

public class and_it_is_the_emulator_certificate : given.certificates
{
    void Because() => _isTrusted = _chain.Build(_trustedRoot);

    [Fact] void should_trust_the_certificate() => _isTrusted.ShouldBeTrue();
}
