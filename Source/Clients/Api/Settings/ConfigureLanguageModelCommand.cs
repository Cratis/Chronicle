// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Settings;

/// <summary>
/// Represents a command to configure a language model.
/// </summary>
/// <param name="Endpoint">The OpenAI endpoint URI.</param>
/// <param name="ApiKey">The API key for authentication.</param>
/// <param name="Model">The language model to use.</param>
public record ConfigureLanguageModelCommand(string Endpoint, string ApiKey, string Model);
