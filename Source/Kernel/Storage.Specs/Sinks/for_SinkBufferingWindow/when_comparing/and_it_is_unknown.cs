// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_SinkBufferingWindow.when_comparing;

public class and_it_is_unknown : Specification
{
    bool _result;

    void Because() => _result = SinkBufferingWindow.IsUnchangedAndClosed(SinkBufferingWindow.Unknown, SinkBufferingWindow.Unknown);

    [Fact] void should_not_be_persisted() => _result.ShouldBeFalse();
}
