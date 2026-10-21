// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the personal data of one event as constraint indexing needs it.
/// </summary>
/// <param name="Subject">The <see cref="Concepts.Events.Subject"/> the personal data belongs to.</param>
/// <param name="Properties">The event's top-level properties that carry personal data.</param>
public record ConstraintPersonalDataOfEvent(Subject Subject, IReadOnlyCollection<string> Properties);
