// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_query_metadata_is_unavailable : given.a_query_operation_policy
{
    bool _withoutServices;
    bool _withoutRegistry;
    bool _unknown;
    bool _mismatched;
    bool _opaque;
    bool _withoutMethod;

    async Task Because()
    {
        Query(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application" });
        _operation = _query with { ServiceProvider = null };
        await AuthorizeOperation();
        _withoutServices = _context.HasSucceeded;
        _operation = _query;
        _services.GetService(typeof(IQueryPerformerProviders)).Returns(null);
        await AuthorizeOperation();
        _withoutRegistry = _context.HasSucceeded;
        _services.GetService(typeof(IQueryPerformerProviders)).Returns(_performers);
        _operation = _query with { Name = "unknown" };
        await AuthorizeOperation();
        _unknown = _context.HasSucceeded;
        _operation = _query;
        _mismatched = await AllowsReturnedPerformer("other-query");
        _opaque = await AllowsReturnedPerformer(_query.Name.Value, opaque: true);
        _withoutMethod = await AllowsReturnedPerformer(_query.Name.Value, true);
    }

    async Task<bool> AllowsReturnedPerformer(string name, bool withoutMethod = false, bool opaque = false)
    {
        var performer = opaque ? Substitute.For<IQueryPerformer>() : Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        if (performer is IAuthorizationQueryTarget target && !withoutMethod) target.AuthorizationMethod.Returns(_performer.AuthorizationMethod);
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName(name));
        _performers.TryGetPerformersFor(_query.Name, out Arg.Any<IQueryPerformer?>()).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        await AuthorizeOperation();
        return _context.HasSucceeded;
    }

    [Fact] void should_refuse_without_request_services() => _withoutServices.ShouldBeFalse();
    [Fact] void should_refuse_without_the_request_registry() => _withoutRegistry.ShouldBeFalse();
    [Fact] void should_refuse_an_unknown_query() => _unknown.ShouldBeFalse();
    [Fact] void should_refuse_a_different_registered_identity() => _mismatched.ShouldBeFalse();
    [Fact] void should_refuse_an_opaque_performer() => _opaque.ShouldBeFalse();
    [Fact] void should_refuse_missing_method_metadata() => _withoutMethod.ShouldBeFalse();
    [Fact] void should_not_use_the_audit_escape_hatch() => _logger.Entries.ShouldBeEmpty();
}
