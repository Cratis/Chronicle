// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents which properties have changed in a webhook definition.
/// </summary>
/// <param name="EventTypesChanged">Whether event types changed.</param>
/// <param name="TargetUrlChanged">Whether target URL changed.</param>
/// <param name="TargetHeadersChanged">Whether target headers changed.</param>
/// <param name="AuthorizationChanged">Whether authorization changed.</param>
/// <param name="IsReplayableChanged">Whether IsReplayable changed.</param>
/// <param name="IsActiveChanged">Whether IsActive changed.</param>
/// <param name="OwnerChanged">Whether Owner changed.</param>
/// <param name="EventSequenceIdChanged">Whether EventSequenceId changed.</param>
public record WebhookDefinitionChangedProperties(
    bool EventTypesChanged,
    bool TargetUrlChanged,
    bool TargetHeadersChanged,
    bool AuthorizationChanged,
    bool IsReplayableChanged,
    bool IsActiveChanged,
    bool OwnerChanged,
    bool EventSequenceIdChanged);
