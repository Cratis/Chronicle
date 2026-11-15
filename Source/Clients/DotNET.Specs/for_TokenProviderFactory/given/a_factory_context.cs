// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.for_TokenProviderFactory.given;

public class a_factory_context : Specification
{
    protected const string WithCredentials = "chronicle://client:secret@localhost:35000";

    protected ITokenProvider _result;

    void Destroy() => (_result as IDisposable)?.Dispose();

    protected ITokenProvider Create(ChronicleOptions options) =>
        TokenProviderFactory.Create(options, () => options.ConnectionString.ServerAddress, false, NullLoggerFactory.Instance);

    protected static ChronicleOptions Options(string connectionString, Authentication? authentication = null) =>
        new(new ChronicleConnectionString(connectionString)) { Authentication = authentication ?? new Authentication() };
}
