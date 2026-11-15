// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs.for_GraphScenario;

public class when_asking_for_something_no_graph_declares : Specification
{
    sealed record Unrelated(Guid Id);

    GraphScenario _scenario;
    Exception _error;

    async Task Because()
    {
        _scenario = new GraphScenario();
        _error = await Catch.Exception(() => _scenario.Node<Unrelated>(Guid.NewGuid()));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_say_that_no_graph_declares_it() => _error.ShouldBeOfExactType<NoGraphDeclaresElement>();
}
