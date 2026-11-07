// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_preserving_raw_serialization_compatibility : Specification
{
    ChronicleConnectionString _original;
    ChronicleConnectionString _roundtripped;
    ChronicleConnectionString _withCredentials;
    string _raw;

    void Establish() => _original = new(a_connection_string_corpus.Raw);

    void Because()
    {
        _raw = _original.ToString();
        _roundtripped = new(_raw);
        _withCredentials = _original.WithCredentials("synthetic-replacement-user", "synthetic-replacement-password");
    }

    [Fact] void should_preserve_raw_output() => _raw.ShouldEqual(a_connection_string_corpus.Raw);
    [Fact] void should_preserve_roundtripped_username() => _roundtripped.Username.ShouldEqual(a_connection_string_corpus.Username);
    [Fact] void should_preserve_roundtripped_password() => _roundtripped.Password.ShouldEqual(a_connection_string_corpus.Password);
    [Fact] void should_preserve_roundtripped_api_key() => _roundtripped.ApiKey.ShouldEqual(a_connection_string_corpus.ApiKey);
    [Fact] void should_preserve_roundtripped_certificate_path() => _roundtripped.CertificatePath.ShouldEqual(a_connection_string_corpus.CertificatePath);
    [Fact] void should_preserve_roundtripped_certificate_password() => _roundtripped.CertificatePassword.ShouldEqual(a_connection_string_corpus.CertificatePassword);
    [Fact] void should_preserve_tls_validation() => _roundtripped.SkipTlsValidation.ShouldBeFalse();
    [Fact] void should_preserve_disabled_tls() => _roundtripped.DisableTls.ShouldBeTrue();
    [Fact] void should_preserve_compatibility_setting() => _roundtripped.SkipCompatibilityCheck.ShouldBeTrue();
    [Fact] void should_preserve_keep_alive_setting() => _roundtripped.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_replace_username() => _withCredentials.Username.ShouldEqual("synthetic-replacement-user");
    [Fact] void should_replace_password() => _withCredentials.Password.ShouldEqual("synthetic-replacement-password");
    [Fact] void should_preserve_api_key_during_credentials_copy() => _withCredentials.ApiKey.ShouldEqual(a_connection_string_corpus.ApiKey);
    [Fact] void should_preserve_certificate_password_during_credentials_copy() => _withCredentials.CertificatePassword.ShouldEqual(a_connection_string_corpus.CertificatePassword);
    [Fact] void should_leave_the_original_unchanged() => _original.ToString().ShouldEqual(_raw);
}
