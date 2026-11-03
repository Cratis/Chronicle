// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

/// <summary>
/// A tag event used by the redaction specs.
/// </summary>
/// <param name="Tag">The tag.</param>
[EventType]
public record RedactableNoteTagged(string Tag);
