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
/// Represents the validator for <see cref="SaveSequenceQueryFolder"/>.
/// </summary>
internal class SaveSequenceQueryFolderValidator : CommandValidator<SaveSequenceQueryFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveSequenceQueryFolderValidator"/> class.
    /// </summary>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the caller.</param>
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    public SaveSequenceQueryFolderValidator(ICurrentPrincipalAccessor currentPrincipalAccessor, IAdministratorAuthorization administratorAuthorization, IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Folder identifier is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.Path).NotEmpty().WithMessage("Folder path is required.");

        // Changing a stored item is only for the owner it is stored under, or an administrator.
        RuleFor(_ => _)
            .MustAsync(async (command, _) =>
            {
                var existing = await storage.GetEventStore(command.EventStore).SequenceQueries.GetFolder(command.Id);
                return await SequenceQueryChangeAccess.IsPermitted(existing?.Owner.Value, currentPrincipalAccessor, administratorAuthorization);
            })
            .When(_ => !string.IsNullOrEmpty(_.EventStore.Value) && !string.IsNullOrEmpty(_.Id.Value))
            .WithMessage(SequenceQueryChangeAccess.FolderRefusal);
    }
}
