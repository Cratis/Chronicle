// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents the validator for <see cref="DeleteSequenceQueryFolder"/>.
/// </summary>
internal class DeleteSequenceQueryFolderValidator : CommandValidator<DeleteSequenceQueryFolder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSequenceQueryFolderValidator"/> class.
    /// </summary>
    public DeleteSequenceQueryFolderValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Folder identifier is required.");
    }
}
