// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Represents a measurement recorded on an instrument.
/// </summary>
/// <param name="Instrument">The name of the instrument.</param>
/// <param name="Value">The recorded value.</param>
/// <param name="Tags">The tags the measurement was recorded with.</param>
public record RecordedMeasurement(string Instrument, int Value, IReadOnlyDictionary<string, object?> Tags);
