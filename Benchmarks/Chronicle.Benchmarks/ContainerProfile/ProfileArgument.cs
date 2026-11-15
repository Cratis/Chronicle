// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>One value-bearing command-line option, retaining its key and raw value together.</summary>
/// <param name="Key">The CLI option name.</param>
/// <param name="Value">The raw option value.</param>
internal sealed record ProfileArgument(string Key, string Value);
