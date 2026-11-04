// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// Verify against a receipt table with one more column than the sink owns. The column is harmless to the receipt inserts
/// unless it is required and has no default and is not generated.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_receipt_table_has_an_extra_column<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _nullable;
    Exception? _requiredWithDefault;
    Exception? _requiredWithoutDefault;
    Exception? _generated;
    ulong _sequenceNumber = 1;

    /// <summary>
    /// Gets the statements that add a nullable column named Extra.
    /// </summary>
    protected abstract IReadOnlyList<string> NullableColumn { get; }

    /// <summary>
    /// Gets the statements that add a NOT NULL column named Extra with a default.
    /// </summary>
    protected abstract IReadOnlyList<string> RequiredColumnWithDefault { get; }

    /// <summary>
    /// Gets the statements that add a NOT NULL column named Extra without a default.
    /// </summary>
    protected abstract IReadOnlyList<string> RequiredColumnWithoutDefault { get; }

    /// <summary>
    /// Gets the statements that add a generated NOT NULL column named Extra.
    /// </summary>
    protected abstract IReadOnlyList<string> GeneratedColumn { get; }

    async Task Establish() => await Seed();

    async Task Because()
    {
        _nullable = await VerifyWith(NullableColumn);
        _requiredWithDefault = await VerifyWith(RequiredColumnWithDefault);
        _requiredWithoutDefault = await VerifyWith(RequiredColumnWithoutDefault);
        _generated = await VerifyWith(GeneratedColumn);
    }

    [Fact] public void should_accept_a_nullable_extra_column() => _nullable.ShouldBeNull();
    [Fact] public void should_accept_a_required_extra_column_with_a_default() => _requiredWithDefault.ShouldBeNull();
    [Fact] public void should_reject_a_required_extra_column_without_a_default() => _requiredWithoutDefault.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_name_the_column_that_is_required_without_a_default() => _requiredWithoutDefault!.Message.ShouldContain("'Extra' is required and has no default");
    [Fact] public void should_accept_a_generated_extra_column() => _generated.ShouldBeNull();

    async Task<Exception?> VerifyWith(IReadOnlyList<string> statements)
    {
        await Provision(AnotherSink(), $"provisioning-{_sequenceNumber}", ++_sequenceNumber);
        foreach (var statement in statements)
        {
            await Execute(statement);
        }

        _verify = true;
        var exception = await Attempt(AnotherSink(), $"verified-{_sequenceNumber}", ++_sequenceNumber);
        _verify = false;
        await DropReceiptTable();
        return exception;
    }
}
