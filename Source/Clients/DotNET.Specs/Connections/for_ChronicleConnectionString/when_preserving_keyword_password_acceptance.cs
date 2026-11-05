// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_preserving_keyword_password_acceptance : Specification
{
    Dictionary<string, ChronicleConnectionStringBuilder> _builders;
    Dictionary<string, ChronicleConnectionString> _connections;

    void Because()
    {
        _builders = a_connection_string_corpus.AllowedAtSignKeys.ToDictionary(key => key, key => new ChronicleConnectionStringBuilder(a_connection_string_corpus.KeywordPassword(key)));
        _connections = a_connection_string_corpus.AllowedAtSignKeys.ToDictionary(key => key, key => new ChronicleConnectionString(a_connection_string_corpus.KeywordPassword(key)));
    }

    [Fact] void should_preserve_the_apparent_host() => _connections.Values.Select(value => value.ServerAddress.Host).ShouldEachConformTo(value => value == a_connection_string_corpus.Username);
    [Fact] void should_preserve_the_apparent_port() => _connections.Values.Select(value => value.ServerAddress.Port).ShouldEachConformTo(value => value == 1753198871);
    [Fact] void should_not_invent_userinfo() => _connections.Values.All(value => value.Username is null).ShouldBeTrue();
    [Fact] void should_not_invent_a_password() => _connections.Values.All(value => value.Password is null).ShouldBeTrue();
    [Fact] void should_preserve_each_query_assignment() => _builders.All(pair => (string)pair.Value[pair.Key] == a_connection_string_corpus.Tail).ShouldBeTrue();
    [Fact] void should_preserve_api_key() => _connections["apiKey"].ApiKey.ShouldEqual(a_connection_string_corpus.Tail);
    [Fact] void should_preserve_certificate_path() => _connections["certificatePath"].CertificatePath.ShouldEqual(a_connection_string_corpus.Tail);
    [Fact] void should_preserve_certificate_password() => _connections["certificatePassword"].CertificatePassword.ShouldEqual(a_connection_string_corpus.Tail);
    [Fact] void should_preserve_load_balancer() => _connections["loadBalancer"].LoadBalancer.ShouldEqual(a_connection_string_corpus.Tail);
    [Fact] void should_preserve_name_server() => _connections["srvNameServer"].SrvNameServer.ShouldEqual(a_connection_string_corpus.Tail);
}
