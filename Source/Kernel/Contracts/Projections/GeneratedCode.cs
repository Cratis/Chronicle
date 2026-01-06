// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents generated C# code from a projection DSL.
/// </summary>
[ProtoContract]
public class GeneratedCode
{
    /// <summary>
    /// Gets or sets the generated C# code.
    /// </summary>
    [ProtoMember(1)]
    public string Code { get; set; } = string.Empty;
}
