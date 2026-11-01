// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer.when_any_property_differs;

/// <summary>
/// Completeness guard: changes each writable property of the row, and of the failed partitions and attempts it
/// holds, one at a time. A column added without updating <see cref="ObserverStateRowComparer"/> fails here.
/// </summary>
public class and_every_property_is_changed_in_turn : Specification
{
    static readonly DateTimeOffset _other = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    List<string> _equalDespiteChange;
    List<string> _unsupported;
    int _checked;

    void Because()
    {
        _equalDespiteChange = [];
        _unsupported = [];
        Check(ComparerRows.Create, row => row);
        Check(ComparerRows.CreateFailedPartition, WithFailedPartition);
        Check(ComparerRows.CreateAttempt, WithAttempt);
    }

    [Fact] void should_have_checked_properties() => (_checked > 40).ShouldBeTrue();
    [Fact] void should_support_every_property_type() => _unsupported.ShouldBeEmpty();
    [Fact] void should_detect_every_property_change() => _equalDespiteChange.ShouldBeEmpty();

    static ObserverState WithFailedPartition(FailedPartition partition)
    {
        var row = ComparerRows.Create();
        row.FailedPartitions = [partition];
        return row;
    }

    static ObserverState WithAttempt(FailedPartitionAttempt attempt)
    {
        return WithFailedPartition(ComparerRows.CreateFailedPartition() with { Attempts = [attempt] });
    }

    static object? OtherValue(Type type, object? current) => type switch
    {
        _ when Nullable.GetUnderlyingType(type) is { } underlying => OtherValue(underlying, current),
        _ when type == typeof(string) => (current as string ?? string.Empty) + "x",
        _ when type == typeof(bool) => !(bool)current!,
        _ when type == typeof(int) => (int)current! + 1,
        _ when type == typeof(long) => (long)current! + 1,
        _ when type == typeof(ulong) => (ulong)current! + 1,
        _ when type == typeof(Guid) => Guid.NewGuid(),
        _ when type == typeof(DateTimeOffset) => _other,
        _ when type.IsEnum => Enum.GetValues(type).Cast<object>().First(value => !value.Equals(current)),
        _ when type == typeof(ObserverId) => new ObserverId("other-observer"),
        _ when type == typeof(ISet<string>) => new HashSet<string> { "other" },
        _ when type == typeof(IDictionary<string, ulong>) => new Dictionary<string, ulong> { ["other"] = 1 },
        _ when type == typeof(IEnumerable<string>) => new[] { "other" },
        _ when type == typeof(IEnumerable<FailedPartition>) => Array.Empty<FailedPartition>(),
        _ when type == typeof(IEnumerable<FailedPartitionAttempt>) => Array.Empty<FailedPartitionAttempt>(),
        _ when type == typeof(FailedPartitionRecoveryCursor) => new FailedPartitionRecoveryCursor(1, FailedPartitionRecoveryMode.Live, null),
        _ when type == typeof(FailedPartitionRecoveryOwner) => new FailedPartitionRecoveryOwner(Guid.NewGuid(), Guid.NewGuid(), 1, default, 1),
        _ when type == typeof(FailedPartitionHold) => new FailedPartitionHold(FailedPartitionHoldReason.WaitingForHandler, "event-type", _other),
        _ when type == typeof(DeliveryFenceContext) => new DeliveryFenceContext(_other, null),
        _ => throw new NotSupportedException(type.FullName)
    };

    void Check<T>(Func<T> create, Func<T, ObserverState> toRow)
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.SetMethod?.IsPublic == true))
        {
            var name = typeof(T).Name + "." + property.Name;
            var changed = create();
            try
            {
                property.SetValue(changed, OtherValue(property.PropertyType, property.GetValue(changed)));
            }
            catch (NotSupportedException)
            {
                _unsupported.Add(name);
                continue;
            }

            _checked++;
            if (ObserverStateRowComparer.Instance.Equals(toRow(create()), toRow(changed)))
            {
                _equalDespiteChange.Add(name);
            }
        }
    }
}
