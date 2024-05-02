// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Describes what should be available in the index file for a module.
/// </summary>
/// <param name="Exports">Files to export.</param>
public record IndexDescriptor(IEnumerable<string> Exports);
