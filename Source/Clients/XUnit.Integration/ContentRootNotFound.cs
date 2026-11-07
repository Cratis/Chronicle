// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// The exception that is thrown when no directory at or above a test assembly holds a project file.
/// </summary>
/// <param name="assemblyLocation">The location of the test assembly.</param>
public class ContentRootNotFound(string assemblyLocation)
    : Exception($"No project file was found at or above the test assembly at '{assemblyLocation}'.");
