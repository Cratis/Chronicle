// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Signals that the expected incarnation is open but the calling activation must reload its state before writing.
/// </summary>
public sealed class NamespaceIncarnationRequiresReactivation() : Exception("The namespace incarnation requires a fresh activation.");
