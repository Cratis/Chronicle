// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The exception that is thrown when the schema generated for a contracts assembly does not parse.
/// </summary>
/// <param name="assemblyPath">The assembly the schema was generated from.</param>
/// <param name="errors">What the parser reported.</param>
public class CouldNotBuildDescriptorSet(string assemblyPath, IEnumerable<string> errors)
    : Exception($"The proto schema generated from '{assemblyPath}' does not parse:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
