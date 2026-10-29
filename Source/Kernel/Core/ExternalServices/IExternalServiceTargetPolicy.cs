// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines destination validation for HTTP capture sources.
/// </summary>
public interface IExternalServiceTargetPolicy : IWebhookTargetPolicy;
