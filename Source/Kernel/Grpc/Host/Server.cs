// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.DevelopmentTools;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Services.Host;

/// <summary>
/// Represents an implementation of <see cref="IServer"/>.
/// </summary>
/// <param name="resetter">The <see cref="KernelStateResetter"/> performing a development reset.</param>
internal sealed class Server(KernelStateResetter resetter) : IServer
{
    /// <inheritdoc/>
    public Task<ServerVersionInfo> GetVersionInfo() =>
        Task.FromResult(new ServerVersionInfo
        {
            Version = ServerVersion.Version,
            CommitSha = ServerVersion.CommitSha,
            ProtocolVersion = Contracts.ProtocolVersion.Current
        });

    /// <inheritdoc/>
    [AllowAnonymous]
    public Task ResetKernelState() => resetter.Reset();
}
