// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

internal sealed class InterleavingTokenProvider(ITokenProvider inner) : ITokenProvider, ITokenCacheBindingProvider, IDisposable
{
    readonly ITokenCacheBindingProvider _bindingProvider = (ITokenCacheBindingProvider)inner;

    internal Func<Task>? AfterAcquisition { get; set; }

    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        (await ((ITokenCacheBindingProvider)this).GetTokenWithBinding(cancellationToken))?.AccessToken;

    public async Task<string?> Refresh(CancellationToken cancellationToken = default) =>
        (await ((ITokenCacheBindingProvider)this).RefreshTokenWithBinding(cancellationToken))?.AccessToken;

    public void Dispose() => (inner as IDisposable)?.Dispose();

    Task<TokenWithBinding?> ITokenCacheBindingProvider.GetTokenWithBinding(CancellationToken cancellationToken) =>
        Interleave(_bindingProvider.GetTokenWithBinding(cancellationToken));

    Task<TokenWithBinding?> ITokenCacheBindingProvider.RefreshTokenWithBinding(CancellationToken cancellationToken) =>
        Interleave(_bindingProvider.RefreshTokenWithBinding(cancellationToken));

    ValueTask<TokenCacheBinding?> ITokenCacheBindingProvider.GetCurrentCacheBinding(CancellationToken cancellationToken) =>
        _bindingProvider.GetCurrentCacheBinding(cancellationToken);

    async Task<TokenWithBinding?> Interleave(Task<TokenWithBinding?> acquisition)
    {
        var token = await acquisition;
        var interleaving = AfterAcquisition;
        AfterAcquisition = null;
        if (interleaving is not null)
        {
            await interleaving();
        }

        return token;
    }
}
