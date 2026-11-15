// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_binding_raw_connection_credentials : Specification
{
    ServiceProvider _provider;
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Chronicle"] = a_connection_string_corpus.Raw
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ChronicleOptions>().WithChronicleConfiguration("Chronicle", "Chronicle");
        _provider = services.BuildServiceProvider();
        _result = _provider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_preserve_raw_connection_text() => _result.ConnectionString.ToString().ShouldEqual(a_connection_string_corpus.Raw);
    [Fact] void should_preserve_username() => _result.ConnectionString.Username.ShouldEqual(a_connection_string_corpus.Username);
    [Fact] void should_preserve_password() => _result.ConnectionString.Password.ShouldEqual(a_connection_string_corpus.Password);
    [Fact] void should_preserve_api_key() => _result.ConnectionString.ApiKey.ShouldEqual(a_connection_string_corpus.ApiKey);
    [Fact] void should_preserve_certificate_path() => _result.ConnectionString.CertificatePath.ShouldEqual(a_connection_string_corpus.CertificatePath);
    [Fact] void should_preserve_certificate_password() => _result.ConnectionString.CertificatePassword.ShouldEqual(a_connection_string_corpus.CertificatePassword);
}
