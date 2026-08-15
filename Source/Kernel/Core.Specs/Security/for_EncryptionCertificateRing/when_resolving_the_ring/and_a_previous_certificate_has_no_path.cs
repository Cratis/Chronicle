// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_a_previous_certificate_has_no_path : given.certificate_files
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
        _ = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath, [null])
        }).All.ToArray());

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EncryptionCertificateWithoutPath>();
}
