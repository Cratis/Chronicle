// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The exception that is thrown when no stable contracts release exists for a major version.
/// </summary>
/// <param name="major">The major version that has no stable release.</param>
public class NoReleaseForMajor(int major)
    : Exception($"No stable Cratis.Chronicle.Contracts release was ever published for major version {major.ToString(CultureInfo.InvariantCulture)}, so there is no baseline to compare against.");
