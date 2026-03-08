// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Concepts.Patching;

/// <summary>
/// Represents a patch that has been applied.
/// </summary>
/// <param name="Name">The name of the patch.</param>
/// <param name="Version">The version the patch applies to.</param>
/// <param name="AppliedAt">When the patch was applied.</param>
public record Patch(string Name, SemanticVersion Version, DateTimeOffset AppliedAt);
