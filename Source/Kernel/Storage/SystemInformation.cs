// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents system information.
/// </summary>
/// <param name="Version">The semantic version.</param>
public sealed record SystemInformation(SemanticVersion Version);
