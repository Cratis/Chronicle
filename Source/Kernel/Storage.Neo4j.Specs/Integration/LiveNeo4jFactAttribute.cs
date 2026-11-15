// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// A fact that is reported as skipped unless a live Neo4j server is configured.
/// </summary>
public sealed class LiveNeo4jFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveNeo4jFactAttribute"/> class.
    /// </summary>
    /// <param name="sourceFilePath">The source file the fact is declared in, supplied by the compiler.</param>
    /// <param name="sourceLineNumber">The line the fact is declared on, supplied by the compiler.</param>
    public LiveNeo4jFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!LiveNeo4j.Enabled)
        {
            Skip = LiveNeo4j.SkipReason;
        }
    }
}
