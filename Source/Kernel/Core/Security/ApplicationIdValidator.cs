// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Storage;
using FluentValidation;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the cross-cutting existence check for <see cref="ApplicationId"/>, applied automatically to every
/// command or query that carries one - except where a command's own validator opts out via
/// <see cref="IConceptRuleBuilder{T, TValue}.IgnoreConceptRules"/> because the property names an application the
/// command itself is creating (e.g. <see cref="AddApplication.Id"/>).
/// </summary>
/// <remarks>
/// Lives beside the Applications commands rather than next to the <see cref="ApplicationId"/> concept itself:
/// <c>Concepts.csproj</c> is a dependency-free primitives project that <c>Storage.csproj</c> references, so this
/// validator - which needs <see cref="IStorage"/> - cannot live there without a circular project reference.
/// </remarks>
public class ApplicationIdValidator : ConceptValidator<ApplicationId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationIdValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to check for the application's existence in.</param>
    public ApplicationIdValidator(IStorage storage) => RuleFor(_ => _.Value)
        .MustAsync(async (value, _) => await storage.System.Applications.GetById(value) is not null)
        .WithMessage("Application does not exist.");
}
