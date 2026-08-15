// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing;

/// <summary>
/// Clear was added to an interface that shipped without it. The default implementation is what keeps that
/// additive: an implementation outside this assembly keeps compiling, and only fails if it is actually asked to
/// clear something - which it could not have been before the member existed.
/// </summary>
public class and_an_implementation_does_not_provide_clear : Specification
{
    given.IStubBuilder _builder;
    Exception? _error;

    void Establish() => _builder = new given.BuilderWithoutClear();

    void Because() => _error = Catch.Exception(() => _builder.Clear(m => m.Note));

    [Fact] void should_report_that_clearing_is_not_supported() => _error.ShouldBeOfExactType<ClearNotSupported>();
    [Fact] void should_name_the_implementation() => _error!.Message.ShouldContain(nameof(given.BuilderWithoutClear));
}
