// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The durability a server ran with.</summary>
/// <param name="Settings">The server settings, named as the server names them.</param>
/// <param name="Class">The class from <see cref="DurabilityClassifier"/>; equal classes are comparable.</param>
public sealed record DurabilitySettings(IReadOnlyDictionary<string, string> Settings, string Class);
