// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// The exception that is thrown when the source a spec hands to an analyzer does not compile.
/// </summary>
/// <remarks>
/// An analyzer reports nothing over source that does not bind, so a snippet with a typo makes every
/// "should not report any diagnostic" spec over it pass while measuring nothing at all.
/// </remarks>
/// <param name="errors">The compiler errors the spec's source produced.</param>
public class SpecSourceDoesNotCompile(IEnumerable<Diagnostic> errors) : Exception(Describe(errors))
{
    static string Describe(IEnumerable<Diagnostic> errors) =>
        $"The source under analysis does not compile, so the analyzer result measures nothing:{Environment.NewLine}" +
        string.Join(Environment.NewLine, errors.Select(error => $"  {error.Id} at {error.Location.GetLineSpan().StartLinePosition}: {error.GetMessage()}"));
}
