// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.Projections.when_redacting_an_event.and_the_engine_uses_the_default_key.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.when_redacting_an_event;

[Collection(ChronicleCollection.Name)]
public class and_the_engine_uses_the_default_key(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : and_projection_is_watermarked.context(fixture)
    {
        protected override bool UseDefaultKey => true;
    }

    [Fact] void should_rebuild_without_the_redacted_content() => Context.AfterRedaction.Content.ShouldEqual("retained content");
}
