// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a syntax error in the projection declaration language.
/// </summary>
[ProtoContract]
public class ProjectionDeclarationSyntaxError
{
    /// <summary>
    /// The error message.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public int Line { get; set; }

    /// <summary>
    /// The column number where the error occurred.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public int Column { get; set; }
}
