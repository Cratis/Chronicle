// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

public class when_waiting_for_protocol_output
{
    [Fact]
    public async Task should_preserve_settlement_received_before_readiness()
    {
        var output = new ProtocolOutput();
        output.Add("SETTLED occurrence first");
        output.Add("READY process");
        output.Add("SETTLED occurrence second");
        output.Complete();

        Assert.Equal("READY process", await output.WaitForLine("READY", CancellationToken.None));
        Assert.Equal("SETTLED occurrence first", await output.WaitForLine("SETTLED occurrence", CancellationToken.None));
        Assert.Equal("SETTLED occurrence second", await output.WaitForLine("SETTLED occurrence", CancellationToken.None));
        Assert.Null(await output.WaitForLine("SETTLED occurrence", CancellationToken.None));
    }

    [Fact]
    public async Task should_wake_a_waiter_without_consuming_unmatched_lines()
    {
        var output = new ProtocolOutput();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ready = output.WaitForLine("READY", deadline.Token);
        output.Add("SETTLED occurrence result");
        output.Add("READY process");

        Assert.Equal("READY process", await ready);
        Assert.Equal("SETTLED occurrence result", await output.WaitForLine("SETTLED", deadline.Token));
    }

    [Fact]
    public async Task should_end_a_wait_when_stdout_closes_without_the_expected_message()
    {
        var output = new ProtocolOutput();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ready = output.WaitForLine("READY", deadline.Token);
        output.Add("SETTLED occurrence result");
        output.Complete();

        Assert.Null(await ready);
        Assert.Equal("SETTLED occurrence result", await output.WaitForLine("SETTLED", deadline.Token));
    }

    [Fact]
    public async Task should_preserve_unmatched_lines_after_cancellation()
    {
        var output = new ProtocolOutput();
        output.Add("SETTLED occurrence result");
        using var deadline = new CancellationTokenSource();
        await deadline.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => output.WaitForLine("READY", deadline.Token));
        Assert.Equal("SETTLED occurrence result", await output.WaitForLine("SETTLED", CancellationToken.None));
    }

    [Fact]
    public async Task should_fail_rather_than_drop_lines_when_the_buffer_is_full()
    {
        var output = new ProtocolOutput();
        for (var index = 0; index < ProtocolOutput.Capacity; index++) output.Add($"LOG {index}");

        Assert.Throws<InvalidOperationException>(() => output.Add("READY process"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => output.WaitForLine("READY", CancellationToken.None));
    }

    [Fact]
    public async Task should_propagate_stdout_failure_to_a_waiter()
    {
        var output = new ProtocolOutput();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ready = output.WaitForLine("READY", deadline.Token);
        var failure = new IOException("Read failed.");
        output.Complete(failure);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ready);
        Assert.Same(failure, exception.InnerException);
    }
}
