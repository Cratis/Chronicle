// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing.given;

/// <summary>
/// Everything the builder interface requires apart from <c>Clear</c>, so the two leaves below differ in exactly
/// one member and nothing else.
/// </summary>
/// <remarks>
/// Hand-written rather than substituted on purpose. A substitute intercepts every interface member, including one
/// with a default implementation, and returns a default instead of running the default body - so a spec built on a
/// substitute would report no exception whether or not the default throws, and would pass on a member that does
/// not exist.
/// </remarks>
public abstract class StubBuilder : IStubBuilder
{
    public IStubBuilder UsingKey<TProperty>(Expression<Func<ClearEvent, TProperty>> keyAccessor) => this;
    public IStubBuilder UsingKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor) => this;
    public IStubBuilder UsingParentKey<TProperty>(Expression<Func<ClearEvent, TProperty>> keyAccessor) => this;
    public IStubBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, ClearEvent>> builderCallback) => this;
    public IStubBuilder UsingParentKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor) => this;
    public IStubBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, ClearEvent>> builderCallback) => this;
    public IStubBuilder UsingConstantKey(string value) => this;
    public IStubBuilder UsingConstantParentKey(string value) => this;
    public IStubBuilder Increment<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => this;
    public IStubBuilder Decrement<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => this;
    public IAddBuilder<ClearReadModel, ClearEvent, TProperty, IStubBuilder> Add<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => null!;
    public ISubtractBuilder<ClearReadModel, ClearEvent, TProperty, IStubBuilder> Subtract<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => null!;
    public IStubBuilder Count<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => this;
    public IStubBuilder AddChild<TChildModel>(Expression<Func<ClearReadModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<ClearEvent, TChildModel>> eventProperty) => this;
    public IStubBuilder AddChild<TChildModel>(Expression<Func<ClearReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, ClearEvent>> builderCallback) => this;
    public ISetBuilder<ClearReadModel, ClearEvent, IStubBuilder> Set(PropertyPath propertyPath) => null!;
    public ISetBuilder<ClearReadModel, ClearEvent, TProperty, IStubBuilder> Set<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor) => null!;
    public ISetBuilder<ClearReadModel, ClearEvent, IStubBuilder> SetThisValue() => null!;
}

/// <summary>
/// An implementation written before <c>Clear</c> existed: it satisfies every other member and leaves <c>Clear</c>
/// to the interface's default implementation.
/// </summary>
public class BuilderWithoutClear : StubBuilder;

/// <summary>
/// An implementation that provides <c>Clear</c> itself, so the default implementation is never reached.
/// </summary>
/// <remarks>
/// <see cref="IStubBuilder"/> is re-declared here even though <see cref="StubBuilder"/> already implements it. That
/// is load-bearing rather than redundant: interface mapping is fixed at the type that names the interface, so a
/// public method added further down the hierarchy does not take over a slot the default implementation already
/// filled. Without the re-declaration, calls through the interface still reach the throwing default.
/// </remarks>
public class BuilderWithClear : StubBuilder, IStubBuilder
{
    public PropertyPath ClearedProperty { get; private set; } = PropertyPath.Root;

    public IStubBuilder Clear<TProperty>(Expression<Func<ClearReadModel, TProperty>> readModelPropertyAccessor)
    {
        readModelPropertyAccessor.TryGetPropertyPath(out var propertyPath);
        ClearedProperty = propertyPath;
        return this;
    }
}

public record ClearReadModel(string? Note);

public record ClearEvent(string Name);

/// <summary>
/// A closed builder interface, so the stubs below can implement the generic builder with concrete type arguments.
/// </summary>
public interface IStubBuilder : IReadModelPropertiesBuilder<ClearReadModel, ClearEvent, IStubBuilder>;
