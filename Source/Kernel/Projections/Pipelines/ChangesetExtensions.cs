// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Extension methods for <see cref="Changeset{TEvent, TState}"/>.
/// </summary>
public static class ChangesetExtensions
{
    /// <summary>
    /// Adds properties from a source type, any existing modifications to these properties will not be included.
    /// </summary>
    /// <param name="changeset">Changeset to add to.</param>
    /// <param name="source">Source to add from.</param>
    public static void AddPropertiesFrom(this IChangeset<AppendedEvent, ExpandoObject> changeset, ExpandoObject source)
    {
        var initialModelStateAsDictionary = source as IDictionary<string, object>;
        var existingProperties = new List<PropertyPath>();

        List<KeyValuePair<string, object>> safeCopy;

        // Note: Since our internals are not necessarily thread safe, we need to reduce the risk of conflicts
        // when copying the key-value pairs. This is a best effort to avoid exceptions due to concurrent modifications.
        try
        {
            // Attempt to copy key-value pairs safely
            safeCopy = new List<KeyValuePair<string, object>>(initialModelStateAsDictionary);
        }
        catch (InvalidOperationException)
        {
            // Handle modification issue by retrying, could still fail - so this is a best effort fallback mechanism
            safeCopy = initialModelStateAsDictionary.ToList();
        }

        existingProperties.AddRange(changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .SelectMany(_ => _.Differences)
            .Select(_ => _.PropertyPath));

        existingProperties.AddRange(changeset.Changes
            .OfType<ChildAdded>()
            .Select(_ => (PropertyPath)_.ChildrenProperty.Segments.First().Value));

        var newProperties = safeCopy
            .Where(_ => !existingProperties.Exists(property => property == _.Key))
            .Select(_ => new PropertyDifference(_.Key, null, _.Value)).ToArray();

        if (newProperties.Length != 0)
        {
            var propertiesChanged = new PropertiesChanged<ExpandoObject>(changeset.CurrentState, newProperties);
            changeset.Add(propertiesChanged);
        }
    }
}
