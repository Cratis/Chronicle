// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.given;

public class ManualTime : TimeProvider
{
    long _timestamp;

    public override long TimestampFrequency => 1000;

    public override long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan span) => _timestamp += (long)span.TotalMilliseconds;
}
