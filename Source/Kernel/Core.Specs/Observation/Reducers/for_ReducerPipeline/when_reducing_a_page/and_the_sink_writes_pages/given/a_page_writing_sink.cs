// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.and_the_sink_writes_pages.given;

public class a_page_writing_sink : when_reducing_a_page.given.a_page_pipeline
{
    protected when_reducing_a_page.given.PageSinkWithPageWrites PageWritingSink => (when_reducing_a_page.given.PageSinkWithPageWrites)_sink;

    protected override when_reducing_a_page.given.PageSink CreateSink() => new when_reducing_a_page.given.PageSinkWithPageWrites();
}
