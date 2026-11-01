// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Represents the validator for <see cref="RegisterTriggers"/>.
/// </summary>
internal class RegisterTriggersValidator : CommandValidator<RegisterTriggers>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTriggersValidator"/> class.
    /// </summary>
    public RegisterTriggersValidator() => RuleFor(command => command.EventStore).NotSystemEventStore();
}
