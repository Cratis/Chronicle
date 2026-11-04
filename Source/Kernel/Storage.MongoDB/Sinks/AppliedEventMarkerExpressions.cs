// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Builds the aggregation expressions a guarded applied event marker update is made of.
/// </summary>
internal static class AppliedEventMarkerExpressions
{
    /// <summary>
    /// Gets the expression that removes a field.
    /// </summary>
    public static readonly BsonValue Remove = "$$REMOVE";

    /// <summary>
    /// Wraps a value so it is taken literally rather than read as a field path or an expression.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The expression.</returns>
    public static BsonDocument Literal(BsonValue value) => new("$literal", value);

    /// <summary>
    /// Gets the expression that holds when a slot does not already record the event, or a later one of the same stream.
    /// </summary>
    /// <param name="slot">The path of the slot.</param>
    /// <param name="stream">The identity of the stream.</param>
    /// <param name="sequenceNumber">The event sequence number.</param>
    /// <returns>The expression.</returns>
    public static BsonDocument NotApplied(string slot, string stream, long sequenceNumber) =>
        new("$or", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", $"${slot}"), "missing" }),
            new BsonDocument("$ne", new BsonArray { $"${slot}.s", Literal(stream) }),
            new BsonDocument("$lt", new BsonArray { $"${slot}.n", Literal(sequenceNumber) })
        });

    /// <summary>
    /// Gets the expression that holds when the document references a join key, and the write is either the one that sets the reference or is newer than the slot.
    /// </summary>
    /// <param name="reference">The expression of the reference the document ends up with.</param>
    /// <param name="onProperty">The property the join is on.</param>
    /// <param name="joinKey">The join key.</param>
    /// <param name="notApplied">The <see cref="NotApplied"/> expression of the join slot.</param>
    /// <returns>The expression.</returns>
    public static BsonDocument ReferencedAndNotApplied(BsonValue reference, string onProperty, BsonValue joinKey, BsonDocument notApplied) =>
        new("$and", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { reference, Literal(joinKey) }),
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("$ne", new BsonArray { $"${onProperty}", Literal(joinKey) }),
                notApplied
            })
        });

    /// <summary>
    /// Gets the expression of a value that is the current value of an object, or an empty object when it is not one.
    /// </summary>
    /// <param name="value">The expression of the value.</param>
    /// <returns>The expression.</returns>
    public static BsonDocument ObjectOrEmpty(BsonValue value) =>
        Let("b", value, new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$$b"), "object" }),
            "$$b",
            Literal(new BsonDocument())
        }));

    /// <summary>
    /// Gets the expression that sets a value at a path below an object.
    /// </summary>
    /// <param name="current">The expression of the current value of the object.</param>
    /// <param name="path">The segments below the object.</param>
    /// <param name="value">The value, or null to remove it.</param>
    /// <returns>The expression.</returns>
    public static BsonValue SetBelow(BsonValue current, IReadOnlyList<string> path, BsonValue? value) => SetBelow(current, path, 0, value);

    /// <summary>
    /// Gets the expression of a value that is written, or of the removal of a field when there is no value.
    /// </summary>
    /// <param name="value">The value, or null to remove.</param>
    /// <returns>The expression.</returns>
    public static BsonValue Written(BsonValue? value) => value is null ? Remove : Literal(value);

    /// <summary>
    /// Gets the expression that applies a write when its predicate holds, and keeps the current value otherwise.
    /// </summary>
    /// <param name="current">The expression of the current value; a field reference or a previous step.</param>
    /// <param name="predicate">The predicate.</param>
    /// <param name="applied">Builds the expression of the value after the write, given a reference to the current value.</param>
    /// <returns>The expression.</returns>
    public static BsonValue When(BsonValue current, BsonValue predicate, Func<BsonValue, BsonValue> applied) =>
        current is BsonString
            ? new BsonDocument("$cond", new BsonArray { predicate, applied(current), current })
            : Let("p", current, new BsonDocument("$cond", new BsonArray { predicate, applied("$$p"), "$$p" }));

    static BsonDocument Let(string name, BsonValue value, BsonValue @in) =>
        new("$let", new BsonDocument { { "vars", new BsonDocument(name, value) }, { "in", @in } });

    static BsonDocument SetBelow(BsonValue current, IReadOnlyList<string> path, int index, BsonValue? value)
    {
        var name = $"o{index}";
        var reference = $"$${name}";
        var segment = Literal(path[index]);
        var inner = index == path.Count - 1
            ? Written(value)
            : SetBelow(new BsonDocument("$getField", new BsonDocument { { "field", segment }, { "input", reference } }), path, index + 1, value);
        return Let(name, ObjectOrEmpty(current), new BsonDocument("$setField", new BsonDocument { { "field", segment }, { "input", reference }, { "value", inner } }));
    }
}
