// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Signals that a protected commit may have succeeded and must not be retried or reported as aborted.
/// </summary>
/// <param name="innerException">The failure observing the commit outcome.</param>
public sealed class NamespaceCommitOutcomeUnknown(Exception innerException)
    : Exception("The namespace-protected event commit outcome is unknown.", innerException);
