// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Sequences;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents the validator for <see cref="SaveSequenceQuery"/>.
/// </summary>
/// <remarks>
/// The filter values, the folder, the occurred bounds and the sort field are deliberately not required -
/// empty means "do not narrow", and an unrecognized sort field falls back to the natural order of the
/// sequence. The event type and tag collections must be present, but may be empty.
/// </remarks>
internal class SaveSequenceQueryValidator : CommandValidator<SaveSequenceQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveSequenceQueryValidator"/> class.
    /// </summary>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the caller.</param>
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    public SaveSequenceQueryValidator(ICurrentPrincipalAccessor currentPrincipalAccessor, IAdministratorAuthorization administratorAuthorization, IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Query identifier is required.");
        RuleFor(_ => _.Name).NotEmpty().WithMessage("Query name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.EventTypes).NotNull().WithMessage("Event types are required.");
        RuleFor(_ => _.Tags).NotNull().WithMessage("Tags are required.");

        // Changing a stored item is only for the owner it is stored under, or an administrator.
        RuleFor(_ => _)
            .MustAsync(async (command, _) =>
            {
                var existing = await storage.GetEventStore(command.EventStore).SequenceQueries.Get(command.Id);
                return await SequenceQueryChangeAccess.IsPermitted(existing?.Owner.Value, currentPrincipalAccessor, administratorAuthorization);
            })
            .When(_ => !string.IsNullOrEmpty(_.EventStore.Value) && !string.IsNullOrEmpty(_.Id.Value))
            .WithMessage(SequenceQueryChangeAccess.QueryRefusal);
    }
}
