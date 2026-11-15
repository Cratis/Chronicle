// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaimValidation.given;

public class a_validation : Specification
{
    protected const string ExternalAuthorityUrl = "https://identity.example";
    protected ExternalAdministratorClaimValidation _validation;
    protected ValidateOptionsResult _result;

    protected static ChronicleOptions Options(ExternalAdministratorClaim? mapping, string? authority = ExternalAuthorityUrl) =>
        new() { Authentication = new Authentication { Authority = authority, ExternalAdministratorClaim = mapping } };

    void Establish() => _validation = new();
}
