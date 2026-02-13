// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// Represents an expected diagnostic for analyzer and code fix tests.
/// </summary>
/// <param name="Id">The diagnostic identifier.</param>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="MessageArguments">The arguments expected in the diagnostic message.</param>
public record ExpectedDiagnostic(string Id, DiagnosticSeverity Severity, params IEnumerable<string> MessageArguments);
