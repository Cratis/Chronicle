// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.given;

public class a_query_operation_policy : an_operation_policy
{
    protected IQueryPerformerProviders _performers;
    protected IServiceProvider _services;
    protected ModelBoundQueryPerformer _performer;
    protected QueryContext _query;

    protected void Query(MethodInfo method, QueryArguments? arguments = null, IServiceProviderIsService? classification = null)
    {
        _performer = new ModelBoundQueryPerformer(method.DeclaringType!, method.DeclaringType!.FullName!, method, classification ?? Substitute.For<IServiceProviderIsService>(), _services.GetService<IAuthorizationEvaluator>() ?? Substitute.For<IAuthorizationEvaluator>());
        _performers.TryGetPerformersFor(_performer.FullyQualifiedName, out Arg.Any<IQueryPerformer?>())
            .Returns(call =>
            {
                call[1] = _performer;
                return true;
            });
        _query = new QueryContext(_performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments, ServiceProvider: _services);
        _operation = _query;
    }

    protected void Query(string method, QueryArguments? arguments = null) => Query(typeof(operation_queries).GetMethod(method)!, arguments);

    protected async Task<bool> Allows(string method, QueryArguments? arguments = null)
    {
        Query(method, arguments);
        await AuthorizeOperation();
        return _context.HasSucceeded;
    }

    void Establish()
    {
        _performers = Substitute.For<IQueryPerformerProviders>();
        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(IQueryPerformerProviders)).Returns(_performers);
        _services.GetService(typeof(IAdministratorAuthorization)).Returns(_authorization);
    }
}
