// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.given;

public class RecordingCredential : TokenCredential
{
    public List<TokenRequestContext> Requests { get; } = [];
    public Func<CancellationToken, ValueTask<AccessToken>> Acquire { get; set; }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        Requests.Add(requestContext);
        return Acquire(cancellationToken);
    }
}
