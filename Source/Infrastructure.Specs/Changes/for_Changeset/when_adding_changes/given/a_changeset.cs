// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes.given;

public class a_changeset : Specification
{
    protected Changeset<object, ExpandoObject> _changeset;
    protected IObjectComparer _comparer;
    protected object _incoming;
    protected ExpandoObject _initialState;

    void Establish()
    {
        _comparer = Substitute.For<IObjectComparer>();
        _incoming = new object();
        _initialState = new ExpandoObject();
        _changeset = new Changeset<object, ExpandoObject>(_comparer, _incoming, _initialState);
    }
}
