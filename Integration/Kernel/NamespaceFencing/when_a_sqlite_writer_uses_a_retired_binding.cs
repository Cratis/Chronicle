// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_sqlite_writer_uses_a_retired_binding.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_sqlite_writer_uses_a_retired_binding(context context) : Given<context>(context)
{
    [Fact] void should_refuse_the_retired_binding() => Context.OldWriterWasRefused.ShouldBeTrue();
    [Fact] void should_not_change_retained_provider_data() => Context.CountAfter.ShouldEqual(Context.CountBefore);
    [Fact] void should_not_write_the_new_marker() => Context.TouchAfter.ShouldEqual(Context.TouchBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_named_namespace(fixture)
    {
        protected override bool UseSqlite => true;
        public bool OldWriterWasRefused;
        public ulong CountBefore;
        public ulong CountAfter;
        public long TouchBefore;
        public long TouchAfter;

        async Task Because()
        {
            (await _oldWriter.AppendOnce("source", new NamespaceFenceValue("old"), "key")).IsSuccess.ShouldBeTrue();
            await Recreate();
            await CurrentWriter();
            CountBefore = await StoredCount();
            TouchBefore = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
            var error = await Catch.Exception(() => _oldWriter.AppendOnce("source", new NamespaceFenceValue("old"), "key"));
            OldWriterWasRefused = IsFenceRefusal(error!);
            CountAfter = await StoredCount();
            TouchAfter = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
        }
    }
}
