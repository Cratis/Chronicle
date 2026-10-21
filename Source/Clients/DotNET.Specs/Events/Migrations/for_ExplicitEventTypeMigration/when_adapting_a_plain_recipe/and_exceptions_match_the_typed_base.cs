// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_exceptions_match_the_typed_base : Specification
{
    IEventTypes _registry;
    ServiceProvider _services;
    Failures _failures;
    Type _typed;
    Exception _constructorFailure;
    Exception _upcastFailure;
    Exception _downcastFailure;
    Exception _mapFailure;
    Exception[] _constructorErrors;
    Exception[] _upcastErrors;
    Exception[] _downcastErrors;
    Exception[] _mapErrors;

    void Establish()
    {
        _registry = Substitute.For<IEventTypes>();
        _registry.GetEventTypeFor(typeof(Previous)).Returns(new EventType("exceptions", 1));
        _registry.GetEventTypeFor(typeof(Current)).Returns(new EventType("exceptions", 2));
        _failures = new();
        _constructorFailure = new Exception("constructor failure");
        _upcastFailure = new Exception("upcast failure");
        _downcastFailure = new Exception("downcast failure");
        _mapFailure = new Exception("map failure");
        _services = new ServiceCollection().AddSingleton(_failures).BuildServiceProvider();
        _typed = given.TypedMigrationCounterpart.For(typeof(Recipe), typeof(Current), typeof(Previous));
    }

    void Because()
    {
        _failures.Constructor = _constructorFailure;
        _constructorErrors = [Catch.Exception(() => CreatePlain()), Catch.Exception(() => CreateTyped())];
        _failures.Constructor = null;
        IEventTypeMigration[] migrations = [CreatePlain(), CreateTyped()];
        _failures.Upcast = _upcastFailure;
        _upcastErrors = migrations.Select(migration => Catch.Exception(() => migration.Upcast(new EventMigrationBuilder()))).ToArray();
        _failures.Upcast = null;
        _failures.Downcast = _downcastFailure;
        _downcastErrors = migrations.Select(migration => Catch.Exception(() => migration.Downcast(new EventMigrationBuilder()))).ToArray();
        _failures.Downcast = null;
        _failures.Map = _mapFailure;
        _mapErrors = migrations.Select(migration => Catch.Exception(() => migration.Upcast(new EventMigrationBuilder()))).ToArray();
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_preserve_the_plain_constructor_exception() => _constructorErrors[0].ShouldBeSame(_constructorFailure);
    [Fact] void should_match_the_typed_constructor_exception() => _constructorErrors[1].ShouldBeSame(_constructorFailure);
    [Fact] void should_preserve_the_plain_upcast_exception() => _upcastErrors[0].ShouldBeSame(_upcastFailure);
    [Fact] void should_match_the_typed_upcast_exception() => _upcastErrors[1].ShouldBeSame(_upcastFailure);
    [Fact] void should_preserve_the_plain_downcast_exception() => _downcastErrors[0].ShouldBeSame(_downcastFailure);
    [Fact] void should_match_the_typed_downcast_exception() => _downcastErrors[1].ShouldBeSame(_downcastFailure);
    [Fact] void should_preserve_the_plain_map_exception() => _mapErrors[0].ShouldBeSame(_mapFailure);
    [Fact] void should_match_the_typed_map_exception() => _mapErrors[1].ShouldBeSame(_mapFailure);

    IEventTypeMigration CreatePlain() => ExplicitMigrationMethods.For(typeof(Recipe)).Create(_services, _registry);
    IEventTypeMigration CreateTyped() => (IEventTypeMigration)ActivatorUtilities.CreateInstance(_services, _typed, _registry);

    public record Previous;
    public record Current;
    public class Failures
    {
        public Exception? Constructor { get; set; }
        public Exception? Upcast { get; set; }
        public Exception? Downcast { get; set; }
        public Exception? Map { get; set; }
    }
    public class Recipe
    {
        readonly Failures _failures;

        public Recipe(Failures failures)
        {
            if (failures.Constructor is { } error) throw error;
            _failures = failures;
        }

        public void Upcast(IEventMigrationBuilder<Current, Previous> builder)
        {
            if (_failures.Upcast is { } error) throw error;
        }

        public void Downcast(IEventMigrationBuilder<Previous, Current> builder)
        {
            if (_failures.Downcast is { } error) throw error;
        }

        public void MapValues(IEventValueMapBuilder<Current, Previous> builder)
        {
            if (_failures.Map is { } error) throw error;
        }
    }
}
