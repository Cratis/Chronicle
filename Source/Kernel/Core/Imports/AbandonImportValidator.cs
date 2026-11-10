// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the validator for <see cref="AbandonImport"/>.
/// </summary>
internal class AbandonImportValidator : CommandValidator<AbandonImport>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbandonImportValidator"/> class.
    /// </summary>
    public AbandonImportValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
    }
}
