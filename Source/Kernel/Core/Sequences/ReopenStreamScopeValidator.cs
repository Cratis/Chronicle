// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Validates manual stream scope repair requests.
/// </summary>
internal class ReopenStreamScopeValidator : CommandValidator<ReopenStreamScope>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReopenStreamScopeValidator"/> class.
    /// </summary>
    /// <param name="principalAccessor">The principal executing the repair.</param>
    public ReopenStreamScopeValidator(ICurrentPrincipalAccessor principalAccessor)
    {
        RuleFor(command => command.EventStore).NotEmpty();
        RuleFor(command => command.Namespace).NotEmpty();
        RuleFor(command => command.EventSequenceId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty();
        RuleFor(command => command).Must(_ => principalAccessor.Current?.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(principalAccessor.Current.FindFirst("sub")?.Value))
            .WithMessage("An authenticated principal with a subject is required.");
    }
}
