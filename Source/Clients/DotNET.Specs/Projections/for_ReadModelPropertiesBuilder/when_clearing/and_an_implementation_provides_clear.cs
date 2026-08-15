// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing;

/// <summary>
/// The other half of the default implementation: an implementation that provides Clear runs its own body, and the
/// throwing default is never reached. Without this the throw could just as well be unconditional.
/// </summary>
/// <remarks>
/// The call goes through the interface on purpose. Whether interface dispatch reaches the implementation or the
/// throwing default is the whole question, and calling the concrete type directly would never ask it - it binds to
/// the class member and passes either way.
/// </remarks>
public class and_an_implementation_provides_clear : Specification
{
    given.BuilderWithClear _builder;
    given.IStubBuilder _result;
    Exception? _error;

    void Establish() => _builder = new given.BuilderWithClear();

    void Because() => _error = Catch.Exception(() => _result = ((given.IStubBuilder)_builder).Clear(m => m.Note));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_run_the_implementation() => _builder.ClearedProperty.Path.ShouldEqual(nameof(given.ClearReadModel.Note));
    [Fact] void should_return_the_builder() => _result.ShouldEqual(_builder);
}
