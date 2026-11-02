// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_reading_settings_with_hostile_values : Specification
{
    const string Marker = "synthetic-query-value-marker";
    const string Overflowing = "99999999999999999999";

    Exception[] _failures;

    void Because() => _failures =
    [
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?port={Marker}").Port),
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?port={Overflowing}").Port),
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?skipTlsValidation={Marker}").SkipTlsValidation),
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?disableTls={Marker}").DisableTls),
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?skipCompatibilityCheck={Marker}").SkipCompatibilityCheck),
        Failure(() => _ = new ChronicleConnectionStringBuilder($"chronicle://localhost?skipKeepAlive={Marker}").SkipKeepAlive)
    ];

    [Fact] void should_still_reject_every_value() => _failures.ShouldEachConformTo(_ => _ is FormatException);
    [Fact] void should_not_expose_the_values_in_messages() => _failures.ShouldEachConformTo(_ => !_.Message.Contains(Marker, StringComparison.Ordinal) && !_.Message.Contains(Overflowing, StringComparison.Ordinal));
    [Fact] void should_not_expose_the_values_in_full_text() => _failures.ShouldEachConformTo(_ => !_.ToString().Contains(Marker, StringComparison.Ordinal) && !_.ToString().Contains(Overflowing, StringComparison.Ordinal));
    [Fact] void should_not_retain_an_inner_exception() => _failures.ShouldEachConformTo(_ => _.InnerException is null);

    static Exception Failure(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        return new InvalidOperationException("No failure");
    }
}
