// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Validates the serverless configuration when the options are resolved, so a host fails at start.
/// </summary>
/// <typeparam name="TOptions">The type of <see cref="ChronicleOptions"/> to validate.</typeparam>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve the hosting environment.</param>
internal sealed class ServerlessOptionsValidator<TOptions>(IServiceProvider serviceProvider) : IValidateOptions<TOptions>
    where TOptions : ChronicleOptions
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (!ServerlessActivation.IsOn(options))
        {
            return ValidateOptionsResult.Success;
        }

        var environment = ServerlessEnvironment.Resolve(serviceProvider);
        var failures = ServerlessConfigurationValidation.Validate(options, environment);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
