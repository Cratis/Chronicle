// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when building a projection definition for a type fails.
/// </summary>
/// <param name="projectionType">The projection type that failed to build.</param>
/// <param name="innerException">The underlying exception that caused the failure.</param>
public class ProjectionDefinitionBuildFailed(Type projectionType, Exception innerException)
    : Exception($"Failed to build projection definition for '{projectionType.FullName}': {innerException.Message}", innerException);
