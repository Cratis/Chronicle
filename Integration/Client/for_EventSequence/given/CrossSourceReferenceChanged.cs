// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Sets a reference value joined into documents on other event sources.
/// </summary>
/// <param name="Amount">The reference amount.</param>
[EventType]
public record CrossSourceReferenceChanged(int Amount);
