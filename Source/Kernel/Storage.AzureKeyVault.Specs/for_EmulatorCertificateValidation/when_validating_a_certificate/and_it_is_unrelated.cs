// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EmulatorCertificateValidation.when_validating_a_certificate;

public class and_it_is_unrelated : given.certificates
{
    void Because() => _isTrusted = _chain.Build(_unrelated);

    [Fact] void should_reject_the_certificate() => _isTrusted.ShouldBeFalse();
}
