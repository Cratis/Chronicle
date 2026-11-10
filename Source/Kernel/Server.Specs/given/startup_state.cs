// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.given;

internal static class startup_state
{
    internal static KernelStartupState For(IHostApplicationLifetime lifetime, ChronicleOptions? options = null, ISingleWriterLease? lease = null) =>
        new(lifetime, Microsoft.Extensions.Options.Options.Create(options ?? new()), lease is null ? [] : [lease], Microsoft.Extensions.Logging.Abstractions.NullLogger<KernelStartupState>.Instance);
}
