// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// The exception that is thrown when an artifact instance that is disposable is activated as non-disposable.
/// </summary>
/// <param name="artifactType">The <see cref="Type"/> of the artifact.</param>
public class ClientArtifactIsDisposable(Type artifactType)
    : Exception($"Activated artifact of type '{artifactType.FullName}' is disposable, but was activated as non-disposable.");
