// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_requiring_skipped_keep_alive;

public class and_the_connection_keeps_alive : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ServerlessActivation.RequireSkippedKeepAlive(Substitute.For<IChronicleConnection>()));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidServerlessConfiguration>();
}
