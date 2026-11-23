// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Signals that the resolved provider destination cannot perform a namespace-protected transaction.
/// </summary>
/// <param name="innerException">The failure establishing transaction support.</param>
public sealed class NamespaceCommitNotSupported(Exception innerException)
    : Exception("Namespace-protected event commits require a transaction-capable storage destination.", innerException);
