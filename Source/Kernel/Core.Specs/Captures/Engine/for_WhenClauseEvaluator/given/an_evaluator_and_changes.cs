// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine.for_WhenClauseEvaluator.given;

public class an_evaluator_and_changes : Specification
{
    protected WhenClauseEvaluator _evaluator;
    protected CaptureChange _addedChange;
    protected CaptureChange _removedChange;
    protected CaptureChange _modifiedChange;

    void Establish()
    {
        _evaluator = new();
        var previous = new JsonObject { ["status"] = "active", ["email"] = "first@example.com", ["age"] = 42 };
        var current = new JsonObject { ["status"] = "inactive", ["email"] = "second@example.com", ["age"] = 42 };
        _addedChange = new("42", CaptureChangeType.Added, null, current.DeepClone().AsObject());
        _removedChange = new("42", CaptureChangeType.Removed, previous.DeepClone().AsObject(), null);
        _modifiedChange = new("42", CaptureChangeType.Modified, previous, current);
    }
}
