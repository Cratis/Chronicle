// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

/// <summary>
/// One read model reached through two separate sink instances over the same database, the way the kernel reaches
/// it: the replay handler, the projection pipeline and the read model store do not share one sink instance.
/// </summary>
public class two_sinks_for_one_read_model : Contract.an_accumulating_read_model<SqlSinkHarness>
{
    protected ISink _otherSink;
    SqlSinkHarness _sqlHarness;

    void Establish() => _otherSink = _sqlHarness.CreateSinkForTheSameReadModel();

    /// <inheritdoc/>
    protected override SqlSinkHarness CreateHarness() => _sqlHarness = new();
}
