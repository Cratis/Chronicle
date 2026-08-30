// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="RemoveUser"/>.
/// </summary>
internal class RemoveUserValidator : CommandValidator<RemoveUser>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveUserValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check the user's existence in.</param>
    /// <remarks>
    /// Removing names a user that has to already exist, unlike <see cref="AddUser"/> which creates one - which is
    /// why the check belongs to this command rather than to the concept.
    /// </remarks>
    public RemoveUserValidator(IStorage storage) =>
        RuleFor(_ => _.UserId).NotEmpty().WithMessage("User identifier is required.")
            .MustAsync(async (userId, _) => await storage.System.Users.GetById(userId) is not null)
            .WithMessage("User does not exist.");
}
