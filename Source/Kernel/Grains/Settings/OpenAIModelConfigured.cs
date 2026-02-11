// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Concepts.Settings;

namespace Cratis.Chronicle.Grains.Settings;

/// <summary>
/// Represents the event for when OpenAI model has been configured.
/// </summary>
/// <param name="Endpoint">The OpenAI endpoint URI.</param>
/// <param name="ApiKey">The API key for authentication.</param>
/// <param name="Model">The language model to use.</param>
[EventType]
public record OpenAIModelConfigured(Uri Endpoint, ApiKey ApiKey, LanguageModel Model);
