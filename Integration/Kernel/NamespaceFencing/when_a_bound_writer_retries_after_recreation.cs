// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_bound_writer_retries_after_recreation.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_bound_writer_retries_after_recreation(context context) : Given<context>(context)
{
    [Fact] void should_refuse_the_old_binding_even_with_a_matching_current_receipt() => Context.OldWriterWasRefused.ShouldBeTrue();
    [Fact] void should_not_treat_the_retired_receipt_as_the_new_append() => Context.FreshWasAlreadyPresent.ShouldBeFalse();
    [Fact] void should_not_duplicate_the_current_lifetime_event() => Context.AfterStaleRetry.ShouldEqual(1UL);
    [Fact] void should_not_touch_the_current_marker_for_the_stale_decision() => Context.TouchAfter.ShouldEqual(Context.TouchBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_named_namespace(fixture)
    {
        public bool OldWriterWasRefused;
        public bool FreshWasAlreadyPresent;
        public ulong AfterStaleRetry;
        public long TouchBefore;
        public long TouchAfter;

        async Task Because()
        {
            (await _oldWriter.AppendOnce("source", new NamespaceFenceValue("same-payload"), "same-key")).IsSuccess.ShouldBeTrue();
            await Recreate();
            var fresh = await CurrentWriter();
            var appended = await fresh.AppendOnce("source", new NamespaceFenceValue("same-payload"), "same-key");
            appended.IsSuccess.ShouldBeTrue();
            FreshWasAlreadyPresent = appended.WasAlreadyPresent;
            TouchBefore = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
            var error = await Catch.Exception(() => _oldWriter.AppendOnce("source", new NamespaceFenceValue("same-payload"), "same-key"));
            OldWriterWasRefused = IsFenceRefusal(error!);
            TouchAfter = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
            AfterStaleRetry = await StoredCount();
        }
    }
}
