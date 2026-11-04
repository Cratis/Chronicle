// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_preserving_raw_serialization_compatibility : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionStringBuilder _fromInheritedText;
    string _raw;
    string _inherited;
    string _connectionString;
    string _afterDiagnostics;

    void Establish() => _builder = new(a_connection_string_corpus.Raw);

    void Because()
    {
        _raw = _builder.Build();
        _inherited = _builder.ToString();
        _connectionString = _builder.ConnectionString;
        _fromInheritedText = new(_connectionString);
        _builder.BuildRedacted();
        _afterDiagnostics = _builder.Build();
    }

    [Fact] void should_preserve_the_raw_url() => _raw.ShouldEqual(a_connection_string_corpus.Raw);
    [Fact] void should_keep_inherited_stringification_raw() => _inherited.ShouldEqual(_connectionString);
    [Fact] void should_preserve_the_username_in_inherited_text() => _inherited.ShouldContain(a_connection_string_corpus.Username);
    [Fact] void should_preserve_the_password_in_inherited_text() => _inherited.ShouldContain(a_connection_string_corpus.Password);
    [Fact] void should_preserve_the_api_key_in_inherited_text() => _inherited.ShouldContain(a_connection_string_corpus.ApiKey);
    [Fact] void should_preserve_the_certificate_password_in_inherited_text() => _inherited.ShouldContain(a_connection_string_corpus.CertificatePassword);
    [Fact] void should_retain_credentials_from_inherited_text() => _fromInheritedText.Password.ShouldEqual(a_connection_string_corpus.Password);
    [Fact] void should_retain_certificate_settings_from_inherited_text() => _fromInheritedText.CertificatePath.ShouldEqual(a_connection_string_corpus.CertificatePath);
    [Fact] void should_not_mutate_raw_serialization_during_diagnostics() => _afterDiagnostics.ShouldEqual(_raw);
}
