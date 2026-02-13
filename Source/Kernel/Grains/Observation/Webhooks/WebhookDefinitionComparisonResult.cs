// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the result of comparing two webhook definitions.
/// </summary>
/// <param name="Result">The <see cref="WebhookDefinitionCompareResult"/>.</param>
/// <param name="ChangedProperties">The properties that changed.</param>
public record WebhookDefinitionComparisonResult(
    WebhookDefinitionCompareResult Result,
    WebhookDefinitionChangedProperties? ChangedProperties);
