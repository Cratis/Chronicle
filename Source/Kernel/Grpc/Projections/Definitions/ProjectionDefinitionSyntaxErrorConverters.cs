// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="CompilerErrors"/>.
/// </summary>
internal static class ProjectionDefinitionSyntaxErrorConverters
{
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
