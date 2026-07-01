// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Defines a language service for compiling capture declaration language.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Compiles a language definition string into a <see cref="CaptureDefinition"/>.
    /// </summary>
    /// <param name="definition">The definition string to compile.</param>
    /// <returns>A compiled capture definition or compiler errors.</returns>
    Result<CaptureDefinition, CompilerErrors> Compile(string definition);
}
