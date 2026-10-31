// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Provides the hard fence when graceful shutdown cannot finish before lease expiry.
/// </summary>
public class SingleWriterLeaseFailFast : ISingleWriterLeaseFailFast
{
    /// <inheritdoc/>
    public void FailFast(string message) => Environment.FailFast(message);
}
