// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Base class for CLI integration spec test classes that use a shared context.
/// </summary>
/// <typeparam name="TContext">The context type containing test setup and results.</typeparam>
/// <param name="context">The context instance.</param>
public class CliGiven<TContext>(TContext context) : IClassFixture<TContext>
    where TContext : class
{
    /// <summary>
    /// Gets the context for the current specification.
    /// </summary>
    protected TContext Context { get; } = context;
}
