// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>The kernel cannot safely supply a supported namespace binding.</summary>
public class NamespaceBindingsNotSupported() : Exception("The kernel does not support namespace binding lookup.");
