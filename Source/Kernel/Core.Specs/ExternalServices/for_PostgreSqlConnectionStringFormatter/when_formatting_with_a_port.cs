// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_PostgreSqlConnectionStringFormatter;

public class when_formatting_with_a_port : Specification
{
    PostgreSqlConnectionStringFormatter _formatter;
    DatabaseEndpointConfiguration _configuration;
    string _result;

    void Establish()
    {
        _formatter = new PostgreSqlConnectionStringFormatter();
        _configuration = new DatabaseEndpointConfiguration(
            "db.example.com",
            5432,
            "Customers",
            "postgres",
            "secret",
            new Dictionary<string, string> { { "SslMode", "Require" } });
    }

    void Because() => _result = _formatter.Format(_configuration);

    [Fact] void should_include_host() => _result.ShouldContain("Host=db.example.com;");
    [Fact] void should_include_port() => _result.ShouldContain("Port=5432;");
    [Fact] void should_include_database() => _result.ShouldContain("Database=Customers;");
    [Fact] void should_include_username() => _result.ShouldContain("Username=postgres;");
    [Fact] void should_include_password() => _result.ShouldContain("Password=secret;");
    [Fact] void should_include_options() => _result.ShouldContain("SslMode=Require;");
}
