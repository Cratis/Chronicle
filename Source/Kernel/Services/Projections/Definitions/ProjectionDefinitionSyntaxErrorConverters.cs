// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="SyntaxError"/>, <see cref="ParsingErrors"/>, and <see cref="CompilerErrors"/>.
/// </summary>
internal static class ProjectionDefinitionSyntaxErrorConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="SyntaxError"/>.
    /// </summary>
    /// <param name="error"><see cref="SyntaxError"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.ProjectionDeclarationSyntaxError ToContract(this SyntaxError error)
    {
        return new()
        {
            Message = error.Message,
            Line = error.Line,
            Column = error.Column
        };
    }

    /// <summary>
    /// Convert to contract version of <see cref="ParsingErrors"/>.
    /// </summary>
    /// <param name="errors"><see cref="ParsingErrors"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.ProjectionDeclarationParsingErrors ToContract(this ParsingErrors errors)
    {
        return new()
        {
            Errors = errors.Errors.Select(e => e.ToContract()).ToList()
        };
    }

    /// <summary>
    /// Convert to contract version of <see cref="CompilerErrors"/>.
    /// </summary>
    /// <param name="errors"><see cref="CompilerErrors"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.ProjectionDeclarationParsingErrors ToContract(this CompilerErrors errors)
    {
        return new()
        {
            Errors = errors.Errors.Select(e => new Contracts.Projections.ProjectionDeclarationSyntaxError
            {
                Message = e.Message,
                Line = e.Line,
                Column = e.Column
            }).ToList()
        };
    }
}
