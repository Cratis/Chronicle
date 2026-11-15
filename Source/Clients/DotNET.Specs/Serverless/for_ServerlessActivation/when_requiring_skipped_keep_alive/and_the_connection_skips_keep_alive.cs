// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_requiring_skipped_keep_alive;

public class and_the_connection_skips_keep_alive : Specification
{
    Exception _error;

    void Because()
    {
        var connection = Substitute.For<IChronicleConnection, IConnectionKeepAlive>();
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);
        _error = Catch.Exception(() => ServerlessActivation.RequireSkippedKeepAlive(connection));
    }

    [Fact] void should_accept() => _error.ShouldBeNull();
}
