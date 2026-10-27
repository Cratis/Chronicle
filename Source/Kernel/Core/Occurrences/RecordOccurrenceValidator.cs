// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents the validator for <see cref="RecordOccurrence"/>.
/// </summary>
internal class RecordOccurrenceValidator : CommandValidator<RecordOccurrence>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordOccurrenceValidator"/> class.
    /// </summary>
    public RecordOccurrenceValidator() => RuleFor(command => command.EventStore).NotSystemEventStore();
}
