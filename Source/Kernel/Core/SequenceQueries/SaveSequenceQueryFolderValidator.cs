// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
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
    public SaveSequenceQueryFolderValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Folder identifier is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.Path).NotEmpty().WithMessage("Folder path is required.");
    }
}
