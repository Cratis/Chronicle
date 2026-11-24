// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when a registration waited in the queue for longer than the configured queue age. The
/// caller is told to retry later instead of waiting for its call to time out.
/// </summary>
/// <param name="retryAfter">How long the caller should wait before it retries.</param>
[GenerateSerializer]
public class RegistrationBusyException(TimeSpan retryAfter) : Exception($"The kernel is busy applying other registrations. Retry after {retryAfter.TotalSeconds:0} seconds.")
{
    /// <summary>
    /// Gets how long the caller should wait before it retries.
    /// </summary>
    [Id(0)]
    public TimeSpan RetryAfter { get; } = retryAfter;
}
