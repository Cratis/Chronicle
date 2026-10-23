// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

public class an_index : Specification
{
    protected readonly ConcurrentDictionary<string, ExpandoObject> _states = new();
    private protected readonly BulkStateChildIndex _index = new();
    protected string[] _matches;

    protected void Set(string key, ExpandoObject document)
    {
        var state = document.Clone();
        _states[key] = state;
        _index.Update(key, state);
    }

    protected void Remove(string key)
    {
        _states.TryRemove(key, out _);
        _index.Remove(key);
    }

    protected string[] Find(PropertyPath path, object value) => _index.Find(path, value, _states).ToArray();

    protected static ExpandoObject Document(params (string Name, object? Value)[] properties)
    {
        var document = new ExpandoObject();
        foreach (var (name, value) in properties)
        {
            ((IDictionary<string, object?>)document)[name] = value;
        }

        return document;
    }

    protected static ExpandoObject Children(params object[] identities) => Document(("children", identities.Select(id => Document(("id", id))).ToList()));
}
