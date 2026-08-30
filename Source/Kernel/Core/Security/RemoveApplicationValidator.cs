// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="RemoveApplication"/>.
/// </summary>
internal class RemoveApplicationValidator : CommandValidator<RemoveApplication>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveApplicationValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check the application's existence in.</param>
    /// <remarks>
    /// Removing names an application that has to already exist, unlike <see cref="AddApplication"/> which creates
    /// one - which is why the check belongs to this command rather than to the concept.
    /// </remarks>
    public RemoveApplicationValidator(IStorage storage) =>
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Application identifier is required.")
            .MustAsync(async (id, _) => await storage.System.Applications.GetById(id) is not null)
            .WithMessage("Application does not exist.");
}
