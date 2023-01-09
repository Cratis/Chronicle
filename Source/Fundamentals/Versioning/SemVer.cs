// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Versioning;

/// <summary>
/// Represents a semantic version number.
/// </summary>
/// <param name="Major">Major version number.</param>
/// <param name="Minor">Minor version number.</param>
/// <param name="Patch">Patch version number.</param>
/// <param name="PreRelease">PreRelease version number.</param>
/// <returns></returns>
public record SemVer(int Major, int Minor, int Patch, string PreRelease);
