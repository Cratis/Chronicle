// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventStreamId;

/// <summary>
/// The client copy of the sentinel check, which used to answer the opposite of what its own doc comment said -
/// true for everything except the default.
/// </summary>
/// <remarks>
/// It survived because nothing consumed it. The kernel has its own copy, which is correct and is what the storage
/// tail filters read, and that one has a spec where this one had none - so the correct copy was pinned and the
/// inverted one was not. It is public API, so a consumer could reach it and get the opposite answer with a
/// correct-looking comment beside it.
/// </remarks>
public class when_checking_the_default_sentinel : Specification
{
    [Fact] void should_report_the_default_as_default() => ((EventStreamId)EventStreamId.Default).IsDefault.ShouldBeTrue();
    [Fact] void should_not_report_another_value_as_default() => new EventStreamId("some-stream").IsDefault.ShouldBeFalse();
}
