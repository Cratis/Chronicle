// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents an implementation of <see cref="IBusinessRules"/>.
/// </summary>
public class BusinessRules : IBusinessRules
{
    readonly IDictionary<Type, IEnumerable<Type>> _businessRulesPerCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRules"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    public BusinessRules(ITypes types)
    {
        var businessRuleTypes = types.All.Where(_ =>
            _.BaseType?.IsGenericType == true &&
            _.BaseType?.GetGenericTypeDefinition() == typeof(BusinessRulesFor<,>)).ToArray();

        _businessRulesPerCommand = businessRuleTypes
            .GroupBy(_ => _.BaseType!.GetGenericArguments()[1])
            .ToDictionary(_ => _.Key, _ => _.ToArray().AsEnumerable());
    }

    /// <inheritdoc/>
    public bool HasFor(Type type) => _businessRulesPerCommand.ContainsKey(type);

    /// <inheritdoc/>
    public IEnumerable<Type> GetFor(Type type) => HasFor(type) ? _businessRulesPerCommand[type] : Array.Empty<Type>();
}
