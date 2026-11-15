// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringConverter;

public class when_roundtripping_raw_credentials : Specification
{
    TypeConverter _converter;
    ChronicleConnectionString _parsed;
    ChronicleConnectionString _roundtripped;
    string _serialized;

    void Establish() => _converter = TypeDescriptor.GetConverter(typeof(ChronicleConnectionString));

    void Because()
    {
        _parsed = (ChronicleConnectionString)_converter.ConvertFromInvariantString(a_connection_string_corpus.Raw)!;
        _serialized = _converter.ConvertToInvariantString(_parsed)!;
        _roundtripped = (ChronicleConnectionString)_converter.ConvertFromInvariantString(_serialized)!;
    }

    [Fact] void should_support_strings_as_input() => _converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
    [Fact] void should_support_strings_as_output() => _converter.CanConvertTo(typeof(string)).ShouldBeTrue();
    [Fact] void should_serialize_raw_credentials() => _serialized.ShouldEqual(a_connection_string_corpus.Raw);
    [Fact] void should_preserve_username() => _roundtripped.Username.ShouldEqual(a_connection_string_corpus.Username);
    [Fact] void should_preserve_password() => _roundtripped.Password.ShouldEqual(a_connection_string_corpus.Password);
    [Fact] void should_preserve_api_key() => _roundtripped.ApiKey.ShouldEqual(a_connection_string_corpus.ApiKey);
    [Fact] void should_preserve_certificate_password() => _roundtripped.CertificatePassword.ShouldEqual(a_connection_string_corpus.CertificatePassword);
}
