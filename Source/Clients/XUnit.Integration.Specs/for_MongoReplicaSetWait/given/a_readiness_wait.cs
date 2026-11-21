// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait.given;

public class a_readiness_wait : Specification
{
    protected IContainer _container;
    private protected readonly readiness_clock _clock = new();
    private protected MongoReplicaSetWait _wait;

    void Establish()
    {
        _container = Substitute.For<IContainer>();
        _container.Id.Returns("mongo-123");
        _container.Image.Returns(new DockerImage("mongo:8.2"));
        _container.ExecAsync(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult("MongoDB rs.initiate exit code: 0\n{ ok: 1, myState: 2 }", "not primary yet", 1));
        _container.GetLogsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(("mongod starting\nreplica-set election stalled", "init failed"));
        _wait = new(_clock);
    }
}
