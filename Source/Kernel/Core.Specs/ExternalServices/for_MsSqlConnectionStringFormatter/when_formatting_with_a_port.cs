// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_MsSqlConnectionStringFormatter;

public class when_formatting_with_a_port : Specification
{
    MsSqlConnectionStringFormatter _formatter;
    DatabaseEndpointConfiguration _configuration;
    string _result;

    void Establish()
    {
        _formatter = new MsSqlConnectionStringFormatter();
        _configuration = new DatabaseEndpointConfiguration(
            "db.example.com",
            1433,
            "Customers",
            "sa",
            "secret",
            new Dictionary<string, string> { { "Encrypt", "True" } });
    }

    void Because() => _result = _formatter.Format(_configuration);

    [Fact] void should_include_server_with_host_and_port() => _result.ShouldContain("Server=db.example.com,1433;");
    [Fact] void should_include_database() => _result.ShouldContain("Database=Customers;");
    [Fact] void should_include_user_id() => _result.ShouldContain("User Id=sa;");
    [Fact] void should_include_password() => _result.ShouldContain("Password=secret;");
    [Fact] void should_include_options() => _result.ShouldContain("Encrypt=True;");
}
