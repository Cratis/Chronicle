// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="Properties.PropertyPath"/> between grains as its path text. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="Path">The path.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.PropertyPathSurrogate")]
public readonly record struct PropertyPathSurrogate([property: Id(0)] string Path);
