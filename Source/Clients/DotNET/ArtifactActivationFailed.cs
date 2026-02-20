// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// The exception that is thrown when an artifact instance could not be activated.
/// </summary>
/// <param name="artifactType">The <see cref="Type"/> of the artifact that failed to activate.</param>
/// <param name="innerException">Optional inner exception.</param>
public class ArtifactActivationFailed(Type artifactType, Exception? innerException = null)
    : Exception($"Failed to activate artifact of type '{artifactType.FullName}'.", innerException);
