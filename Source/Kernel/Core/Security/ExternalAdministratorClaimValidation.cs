// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Fails startup when the external administrator claim mapping is malformed or cannot apply.
/// Messages name settings only and never echo configured values.
/// </summary>
public class ExternalAdministratorClaimValidation : IValidateOptions<ChronicleOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ChronicleOptions options)
    {
        var mapping = options.Authentication.ExternalAdministratorClaim;
        if (mapping is null) return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (!IsExact(mapping.Type)) failures.Add("Authentication:ExternalAdministratorClaim:Type must be a non-empty exact value without surrounding whitespace.");
        if (!IsExact(mapping.Value)) failures.Add("Authentication:ExternalAdministratorClaim:Value must be a non-empty exact value without surrounding whitespace.");
        if (options.Authentication.UseInternalAuthority) failures.Add("Authentication:ExternalAdministratorClaim requires Authentication:Authority to be configured.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    static bool IsExact(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length == value.Length;
}
