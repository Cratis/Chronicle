// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

/// <summary>
/// Records a customer's name to exercise the first append observed by pattern capture.
/// </summary>
/// <param name="Name">The customer's recorded name.</param>
[EventType]
public record CustomerNamed(string Name);
