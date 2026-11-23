// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Signals that the supplied incarnation has no open namespace marker; no protected write was committed.
/// </summary>
public sealed class NamespaceCommitRejected() : Exception("The expected namespace incarnation is not open.");
