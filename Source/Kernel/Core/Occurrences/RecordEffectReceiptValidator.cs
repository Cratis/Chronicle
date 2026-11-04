// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents the validator for <see cref="RecordEffectReceipt"/>.
/// </summary>
internal class RecordEffectReceiptValidator : CommandValidator<RecordEffectReceipt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordEffectReceiptValidator"/> class.
    /// </summary>
    public RecordEffectReceiptValidator() => RuleFor(command => command.EventStore).NotSystemEventStore();
}
