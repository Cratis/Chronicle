// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// An explicit, flow-local read region. It never grants an append permission.
/// </summary>
internal sealed class ReadProtectionReadRegion : IDisposable
{
    static readonly AsyncLocal<ReadProtectionReadRegion?> _current = new();
    readonly ReadProtectionReadRegion? _previous;
    readonly ReadProtectionFrame? _frame;
    readonly ReadAllowance _allowance;
    readonly bool _suspends;
    bool _active = true;

    /// <summary>
    /// Opens a region on this flow's frame, if any.
    /// </summary>
    /// <param name="allowance">The classification of reads in the region.</param>
    /// <param name="reason">The explicit reason for the allowance.</param>
    internal ReadProtectionReadRegion(ReadAllowance allowance, string reason)
    {
        _frame = ReadProtectionFrame.Current;
        _allowance = allowance;
        Reason = reason;
        _previous = _current.Value;
        _current.Value = this;
    }

    ReadProtectionReadRegion()
    {
        _frame = ReadProtectionFrame.Current;
        _previous = _current.Value;
        _suspends = true;
        Reason = "Reducer callback";
        _current.Value = this;
    }

    /// <summary>
    /// Gets whether an advisory region is open for the active invocation.
    /// </summary>
    internal static bool IsAdvisory => Has(ReadAllowance.Advisory);

    /// <summary>
    /// Gets whether a read allowance is open for the active invocation.
    /// </summary>
    internal static bool IsAllowed => IsAdvisory || Has(ReadAllowance.Contributed);

    /// <summary>
    /// Gets the reason retained for instrumentation.
    /// </summary>
    internal string Reason { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        Volatile.Write(ref _active, false);
        if (ReferenceEquals(_current.Value, this)) _current.Value = _previous;
    }

    /// <summary>Suspends inherited read allowances without removing the enforcement frame.</summary>
    /// <returns>The flow-local suspension to dispose after the callback.</returns>
    internal static IDisposable Suspend() => new ReadProtectionReadRegion();

    static bool Has(ReadAllowance allowance)
    {
        var frame = ReadProtectionFrame.Current;
        for (var region = _current.Value; region is not null; region = region._previous)
        {
            if (!Volatile.Read(ref region._active) || !ReferenceEquals(region._frame, frame)) continue;
            if (region._suspends) return false;
            if (region._allowance == allowance) return true;
        }
        return false;
    }
}
