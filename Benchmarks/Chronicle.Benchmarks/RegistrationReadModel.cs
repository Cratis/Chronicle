// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Gives each registration benchmark reducer a separate read model identity.
/// </summary>
/// <typeparam name="TLeft">The first identity marker.</typeparam>
/// <typeparam name="TRight">The second identity marker.</typeparam>
public class RegistrationReadModel<TLeft, TRight>
{
    /// <summary>
    /// Gets or sets the number of reduced events.
    /// </summary>
    public long Count { get; set; }
}
