// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

/// <summary>
/// A fact that is reported as skipped on a platform that cannot negotiate TLS 1.3.
/// </summary>
public sealed class Tls13FactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tls13FactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file the fact is declared in, supplied by the compiler.</param>
    /// <param name="sourceLineNumber">The line the fact is declared on, supplied by the compiler.</param>
    public Tls13FactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = Tls13.SkipReason;
    }
}
