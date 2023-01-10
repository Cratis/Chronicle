// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents the base type for a validator of commands.
/// </summary>
/// <typeparam name="T">Type of command.</typeparam>
public class CommandValidator<T> : AbstractValidator<T>
{
}

/// <summary>
/// Represents a <see cref="IModelValidator"/> for <see cref="CommandValidator{T}"/>.
/// </summary>
public class CommandModelValidator : IModelValidator
{
    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var failures = new List<ModelValidationResult>();

        return failures;
    }
}

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for <see cref="CommandValidator{T}"/>.
/// </summary>
public class CommandModelValidatorProvider : IModelValidatorProvider
{
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of the validators.</param>
    public CommandModelValidatorProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context) => throw new NotImplementedException();
}
