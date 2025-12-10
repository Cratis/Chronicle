// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes.given;

public class a_changeset_with_parent : Specification
{
    protected Changeset<object, ExpandoObject> _parentChangeset;
    protected Changeset<object, ExpandoObject> _childChangeset;
    protected IObjectComparer _comparer;
    protected object _incoming;
    protected ExpandoObject _parentInitialState;
    protected ExpandoObject _childInitialState;

    void Establish()
    {
        _comparer = Substitute.For<IObjectComparer>();
        _incoming = new object();
        _parentInitialState = new ExpandoObject();
        _childInitialState = new ExpandoObject();
        _parentChangeset = new Changeset<object, ExpandoObject>(_comparer, _incoming, _parentInitialState);
        _childChangeset = new Changeset<object, ExpandoObject>(_comparer, _incoming, _childInitialState, _parentChangeset);
    }
}
