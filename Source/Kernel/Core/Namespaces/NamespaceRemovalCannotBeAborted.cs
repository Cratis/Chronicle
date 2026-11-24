// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>The removal cannot be undone without risking a destructive or unknown effect.</summary>
public class NamespaceRemovalCannotBeAborted() : Exception("The namespace removal cannot safely be aborted.");
