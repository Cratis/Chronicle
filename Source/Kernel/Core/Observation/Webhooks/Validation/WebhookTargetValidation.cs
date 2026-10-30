// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// The policy evaluation of a target, without changing its stored definition or activation state.
/// </summary>
/// <param name="IsAllowed">Whether registration or delivery may proceed.</param>
/// <param name="Reason">A safe, local explanation; also populated for warnings.</param>
public record WebhookTargetValidation(bool IsAllowed, string Reason)
{
    /// <summary>
    /// A target with no policy violations.
    /// </summary>
    public static readonly WebhookTargetValidation Allowed = new(true, string.Empty);

    /// <summary>
    /// Gets whether infrastructure prevented a definitive policy decision and validation should be retried.
    /// </summary>
    internal bool IsIndeterminate { get; init; }
}
