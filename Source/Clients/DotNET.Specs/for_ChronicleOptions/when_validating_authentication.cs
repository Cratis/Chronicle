// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_validating_authentication : Specification
{
    const string WithCredentials = "chronicle://client:secret@localhost:35000";
    const string WithoutCredentials = "chronicle://localhost:35000";
    const string Secretive = "https://user:hunter2@idp.example.com/token";

    ValidationResult[] _defaultErrors;
    ValidationResult[] _authorityErrors;
    ValidationResult[] _tokenEndpointErrors;
    ValidationResult[] _httpAuthorityErrors;
    ValidationResult[] _relativeTokenEndpointErrors;
    ValidationResult[] _userInfoTokenEndpointErrors;
    ValidationResult[] _scopeOnlyErrors;
    ValidationResult[] _noCredentialsErrors;
    ValidationResult[] _developmentCredentialsErrors;
    ValidationResult[] _apiKeyErrors;
    ValidationResult[] _noAuthenticationErrors;

    void Because()
    {
        _defaultErrors = Validate(new(), WithCredentials);
        _authorityErrors = Validate(new() { Authority = "https://idp.example.com/realms/chronicle" }, WithCredentials);
        _tokenEndpointErrors = Validate(new() { TokenEndpoint = "https://idp.example.com/token" }, WithCredentials);
        _httpAuthorityErrors = Validate(new() { Authority = "http://idp.example.com" }, WithCredentials);
        _relativeTokenEndpointErrors = Validate(new() { TokenEndpoint = "/connect/token" }, WithCredentials);
        _userInfoTokenEndpointErrors = Validate(new() { TokenEndpoint = Secretive }, WithCredentials);
        _scopeOnlyErrors = Validate(new() { Scope = "api://chronicle/.default" }, WithCredentials);
        _noCredentialsErrors = Validate(new() { TokenEndpoint = "https://idp.example.com/token" }, WithoutCredentials);
        _developmentCredentialsErrors = Validate(new() { TokenEndpoint = "https://idp.example.com/token" }, "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000");
        _apiKeyErrors = Validate(new() { TokenEndpoint = "https://idp.example.com/token" }, "chronicle://localhost:35000?apiKey=k");
        _noAuthenticationErrors = Validate(new() { TokenEndpoint = "https://idp.example.com/token" }, "chronicle://localhost:35000?auth=none");
    }

    [Fact] void should_accept_defaults() => _defaultErrors.ShouldBeEmpty();
    [Fact] void should_accept_an_https_authority() => _authorityErrors.ShouldBeEmpty();
    [Fact] void should_accept_an_https_token_endpoint() => _tokenEndpointErrors.ShouldBeEmpty();
    [Fact] void should_reject_an_http_authority() => _httpAuthorityErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_a_relative_token_endpoint() => _relativeTokenEndpointErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_a_token_endpoint_with_user_info() => _userInfoTokenEndpointErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_a_scope_without_an_external_endpoint() => _scopeOnlyErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_an_external_endpoint_without_credentials() => _noCredentialsErrors.Length.ShouldEqual(1);
    [Fact] void should_reject_an_external_endpoint_with_only_the_development_credentials() => _developmentCredentialsErrors.Length.ShouldEqual(1);
    [Fact] void should_ignore_the_endpoint_for_an_api_key() => _apiKeyErrors.ShouldBeEmpty();
    [Fact] void should_ignore_the_endpoint_for_no_authentication() => _noAuthenticationErrors.ShouldBeEmpty();
    [Fact] void should_not_include_the_url_in_errors() => new[] { _httpAuthorityErrors, _relativeTokenEndpointErrors, _userInfoTokenEndpointErrors, _noCredentialsErrors }
        .SelectMany(_ => _)
        .Any(_ => _.ErrorMessage!.Contains("idp.example.com", StringComparison.Ordinal) || _.ErrorMessage.Contains("hunter2", StringComparison.Ordinal) || _.ErrorMessage.Contains("/connect/token", StringComparison.Ordinal))
        .ShouldBeFalse();

    static ValidationResult[] Validate(Authentication authentication, string connectionString)
    {
        var options = new ChronicleOptions(new ChronicleConnectionString(connectionString)) { Authentication = authentication };
        return options.Validate(new ValidationContext(options)).ToArray();
    }
}
