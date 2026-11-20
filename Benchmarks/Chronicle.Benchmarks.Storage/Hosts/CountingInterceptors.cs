// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>Creates the interceptors that count SQL round trips without wrapping the provider's connection.</summary>
internal static class CountingInterceptors
{
    /// <summary>Creates the command and transaction interceptors for a counter.</summary>
    /// <param name="counter">The counter.</param>
    /// <returns>The interceptors.</returns>
    public static IInterceptor[] For(CommandCounter counter) => [new CountingCommandInterceptor(counter), new CountingTransactionInterceptor(counter)];
}
