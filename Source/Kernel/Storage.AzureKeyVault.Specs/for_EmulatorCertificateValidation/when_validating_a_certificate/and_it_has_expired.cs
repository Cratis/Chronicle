// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EmulatorCertificateValidation.when_validating_a_certificate;

public class and_it_has_expired : given.certificates
{
    void Establish() => _chain.ChainPolicy = EmulatorCertificateValidation.CreatePolicy(_expired);

    void Because() => _isTrusted = _chain.Build(_expired);

    [Fact] void should_reject_the_certificate_even_if_trusted() => _isTrusted.ShouldBeFalse();
}
