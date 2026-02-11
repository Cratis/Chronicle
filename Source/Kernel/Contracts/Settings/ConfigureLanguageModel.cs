// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Settings;

/// <summary>
/// Represents the command for configuring a language model.
/// </summary>
[ProtoContract]
public record ConfigureLanguageModel
{
    /// <summary>
    /// Gets or sets the OpenAI endpoint URI.
    /// </summary>
    [ProtoMember(1)]
    public string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    [ProtoMember(2)]
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the language model to use.
    /// </summary>
    [ProtoMember(3)]
    public string Model { get; set; }
}
