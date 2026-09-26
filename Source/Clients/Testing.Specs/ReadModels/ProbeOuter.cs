// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>The outer object containing optional project information.</summary>
/// <param name="Name">The outer name.</param>
/// <param name="Info">The nested information.</param>
public record ProbeOuter(string Name, ProjectInfo? Info);
