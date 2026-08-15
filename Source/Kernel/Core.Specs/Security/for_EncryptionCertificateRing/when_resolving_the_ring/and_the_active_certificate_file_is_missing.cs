// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_the_active_certificate_file_is_missing : given.certificate_files
{
    string _missingPath;
    EncryptionCertificateRing _ring;
    Exception _exception;

    void Establish() => _missingPath = PathThatHoldsNoFile();

    void Because()
    {
        _ring = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_missingPath)
        });
        _exception = Catch.Exception(() => _ = _ring.All.ToArray());
    }

    [Fact] void should_still_report_the_ring_as_configured() => _ring.IsConfigured.ShouldBeTrue();
    [Fact] void should_fail_closed_rather_than_run_without_the_certificate() => _exception.ShouldBeOfExactType<EncryptionCertificateFileNotFound>();
    [Fact] void should_name_the_path_that_holds_no_file() => _exception.Message.ShouldContain(_missingPath);
}
