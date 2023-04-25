// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Read.EventSequences.PivotViewer;

#pragma warning disable SA1600

public class DateTimeValue
{
    public string Value { get; init; } = DateTimeOffset.UtcNow.ToString();

    public DateTimeValue(DateTimeOffset value)
    {
        Value = value.ToString("o");
    }
}
