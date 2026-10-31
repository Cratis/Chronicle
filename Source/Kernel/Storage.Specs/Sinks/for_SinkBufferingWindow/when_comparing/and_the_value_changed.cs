// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_SinkBufferingWindow.when_comparing;

public class and_the_value_changed : Specification
{
    bool _result;

    void Because() => _result = SinkBufferingWindow.IsUnchangedAndClosed(new(4), new(6));

    [Fact] void should_not_be_persisted() => _result.ShouldBeFalse();
}
