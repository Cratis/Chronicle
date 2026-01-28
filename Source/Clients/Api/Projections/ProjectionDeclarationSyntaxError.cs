// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a syntax error in the projection declaration.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
/// <param name="Column">The column number where the error occurred.</param>
public record ProjectionDeclarationSyntaxError(string Message, int Line, int Column);

