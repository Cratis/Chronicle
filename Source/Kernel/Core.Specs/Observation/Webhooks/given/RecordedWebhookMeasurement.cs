// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.given;

/// <summary>
/// A recorded circuit measurement.
/// </summary>
/// <param name="Instrument">The instrument name.</param>
/// <param name="Value">The measured value.</param>
/// <param name="Tags">The measurement tags.</param>
public record RecordedWebhookMeasurement(string Instrument, long Value, IReadOnlyDictionary<string, object?> Tags);
