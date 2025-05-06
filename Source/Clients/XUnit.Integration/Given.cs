// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a base class for specification by example type of context setups.
/// </summary>
/// <typeparam name="TSetup">Type of context.</typeparam>
public class Given<TSetup> : IClassFixture<TSetup>
    where TSetup : IntegrationSpecificationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Given{TSetup}"/> class.
    /// </summary>
    /// <param name="context">Context to use.</param>
    public Given(TSetup context)
    {
        Context = context;
        context.SetName(GetType().FullName!);
    }

    /// <summary>
    /// Gets the context for the current specification.
    /// </summary>
    public TSetup Context { get; }
}
