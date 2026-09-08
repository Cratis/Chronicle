// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the cross-cutting existence check for <see cref="UserId"/>, applied automatically to every command
/// or query that carries one - except where a command's own validator opts out via
/// <see cref="IConceptRuleBuilder{T, TValue}.IgnoreConceptRules"/> because the property names a user the command
/// itself is creating (e.g. <see cref="AddUser.UserId"/>).
/// </summary>
/// <remarks>
/// Lives beside the Users commands rather than next to the <see cref="UserId"/> concept itself: <c>Concepts.csproj</c>
/// is a dependency-free primitives project that <c>Storage.csproj</c> references, so this validator - which needs
/// <see cref="IStorage"/> - cannot live there without a circular project reference.
/// </remarks>
public class UserIdValidator : ConceptValidator<UserId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the user's existence in.</param>
    public UserIdValidator(IStorage storage) => RuleFor(_ => _.Value)
        .MustAsync(async (value, _) => await storage.System.Users.GetById(value) is not null)
        .WithMessage("User does not exist.");
}
