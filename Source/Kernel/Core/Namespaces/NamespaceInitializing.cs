// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>The namespace has not finished initializing and cannot yet admit the operation.</summary>
public class NamespaceInitializing() : Exception("The namespace is initializing.");
