// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>The captured namespace binding cannot be used for this writer or unit of work.</summary>
public class NamespaceBindingNotValid() : Exception("An open namespace binding for the same event store and namespace is required; an existing binding cannot be replaced.");
