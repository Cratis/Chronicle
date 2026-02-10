// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Settings;

/// <summary>
/// Represents an OpenAI language model provider configuration.
/// </summary>
/// <param name="Endpoint">The OpenAI endpoint URI.</param>
/// <param name="ApiKey">The API key for authentication.</param>
/// <param name="Model">The language model to use.</param>
public record OpenAIProvider(Uri Endpoint, ApiKey ApiKey, LanguageModel Model);
