// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_a_previous_certificate_file_is_missing : given.certificate_files
{
    string _missingPath;
    Exception _exception;

    void Establish() => _missingPath = PathThatHoldsNoFile();

    void Because() => _exception = Catch.Exception(() =>
        _ = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath, _missingPath)
        }).All.ToArray());

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EncryptionCertificateFileNotFound>();
    [Fact] void should_name_the_path_that_holds_no_file() => _exception.Message.ShouldContain(_missingPath);
}
