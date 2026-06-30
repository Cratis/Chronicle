// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a textual label, keyed by a <see cref="string"/>.
/// </summary>
/// <param name="Label">The label, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record LabelRecorded(string Label, decimal Amount);
