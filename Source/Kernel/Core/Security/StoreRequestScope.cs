// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cratis.Chronicle.Security;

/// <summary>Reads the storage scope selected by a command.</summary>
internal static class StoreRequestScope
{
    static readonly ConcurrentDictionary<Type, Func<object, bool>> _application = new();

    /// <summary>Tests whether the command selects event-store storage.</summary>
    /// <param name="command">The command.</param>
    /// <returns>Whether application scope is selected.</returns>
    internal static bool IsApplication(object command) => _application.GetOrAdd(command.GetType(), static type =>
    {
        var property = type.GetProperty("ApplicationScope");
        if (property?.PropertyType != typeof(bool)) return static _ => false;
        var parameter = Expression.Parameter(typeof(object));
        return Expression.Lambda<Func<object, bool>>(Expression.Property(Expression.Convert(parameter, type), property), parameter).Compile();
    })(command);
}
