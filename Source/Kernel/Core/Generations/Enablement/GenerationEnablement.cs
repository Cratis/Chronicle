// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Reads opt-in without touching storage.
/// </summary>
/// <param name="options">The server configuration.</param>
public sealed class GenerationEnablement(IOptions<ChronicleOptions> options) : IGenerationEnablement
{
    /// <inheritdoc/>
    public bool Enabled => options.Value.Features.ProjectionGenerations;
}
