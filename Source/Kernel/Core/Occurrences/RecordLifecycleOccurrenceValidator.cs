// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents the validator for <see cref="RecordLifecycleOccurrence"/>.
/// </summary>
internal class RecordLifecycleOccurrenceValidator : CommandValidator<RecordLifecycleOccurrence>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordLifecycleOccurrenceValidator"/> class.
    /// </summary>
    public RecordLifecycleOccurrenceValidator() => RuleFor(command => command.EventStore).NotSystemEventStore();
}
