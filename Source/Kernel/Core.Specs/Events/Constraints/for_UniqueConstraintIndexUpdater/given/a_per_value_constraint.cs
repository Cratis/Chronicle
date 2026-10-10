// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.given;

public class a_per_value_constraint : Specification
{
    protected IUniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;
    protected EventSourceId _owner;
    protected UniqueConstraintIndexUpdater _updater;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _owner = EventSourceId.New();
        _definition = new("versions", [new("added", ["Id"])], ["removed"])
        {
            Mode = UniqueConstraintMode.PerValue
        };
    }

    protected void Configure(EventTypeId eventType, string property, string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[property] = value;
        var context = new ConstraintValidationContext([], _owner, eventType, content);
        _updater = new(_definition, context, _storage);
    }
}
