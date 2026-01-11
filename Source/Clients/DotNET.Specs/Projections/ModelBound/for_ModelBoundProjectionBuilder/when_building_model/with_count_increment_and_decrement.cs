// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model;

public class with_count_increment_and_decrement : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(UserRegistered),
            typeof(UserLoggedIn),
            typeof(UserLoggedOut),
            typeof(PasswordChanged)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(UserStats));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_from_definition_for_user_logged_in()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserLoggedIn)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_user_logged_out()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserLoggedOut)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_password_changed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PasswordChanged)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_count_password_changes()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PasswordChanged)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(UserStats.PasswordChangeCount)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }

    [Fact]
    void should_increment_active_sessions_on_login()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserLoggedIn)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(UserStats.ActiveSessions)];
        expression.ShouldEqual($"{WellKnownExpressions.Increment}()");
    }

    [Fact]
    void should_decrement_active_sessions_on_logout()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserLoggedOut)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(UserStats.ActiveSessions)];
        expression.ShouldEqual($"{WellKnownExpressions.Decrement}()");
    }

    [Fact]
    void should_count_total_logins()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserLoggedIn)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(UserStats.TotalLogins)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }
}

[EventType]
public record UserRegistered(Guid UserId, string Username);

[EventType]
public record UserLoggedIn();

[EventType]
public record UserLoggedOut();

[EventType]
public record PasswordChanged();

public record UserStats(
    [Key]
    Guid Id,

    [SetFrom<UserRegistered>(nameof(UserRegistered.Username))]
    string Username,

    [Increment<UserLoggedIn>]
    [Decrement<UserLoggedOut>]
    int ActiveSessions,

    [Count<UserLoggedIn>]
    int TotalLogins,

    [Count<PasswordChanged>]
    int PasswordChangeCount);
