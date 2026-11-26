// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.given;

public class composite_children : Specification
{
    protected IList<object?> _children;
    protected ExpandoObject _target;
    protected ExpandoObject _sameTenant;
    protected ExpandoObject _sameTarget;

    void Establish()
    {
        _sameTenant = Child("tenant", "other");
        _target = Child("tenant", "target");
        _sameTarget = Child("other", "target");
        _children = new List<object?> { _sameTenant, _target, _sameTarget };
    }

    protected static ExpandoObject Key(string tenant = "tenant", string target = "target")
    {
        dynamic key = new ExpandoObject();
        key.tenant = tenant;
        key.target = target;
        return key;
    }

    protected static ExpandoObject Child(string tenant, string target)
    {
        dynamic child = new ExpandoObject();
        child.id = Key(tenant, target);
        child.name = "original";
        return child;
    }
}
