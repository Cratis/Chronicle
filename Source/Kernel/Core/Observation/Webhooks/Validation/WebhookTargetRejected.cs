// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// A webhook request was blocked by local target validation.
/// </summary>
/// <param name="reason">The safe, locally generated reason.</param>
public class WebhookTargetRejected(string reason) : HttpRequestException(reason);
