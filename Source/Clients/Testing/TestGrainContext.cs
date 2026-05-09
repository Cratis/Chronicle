// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a minimal <see cref="IGrainContext"/> for constructing grains outside an Orleans silo.
/// </summary>
/// <remarks>
/// The <see cref="Grain"/> base class constructor accesses <c>RuntimeContext.Current!.ObservableLifecycle</c>
/// during construction. By setting this context as the current runtime context before creating the grain,
/// the constructor receives a valid <see cref="IGrainLifecycle"/> without requiring a full Orleans silo.
/// </remarks>
#pragma warning disable SA1648 // inheritdoc should be used with inheriting class — IGrainContext is an interface, not a base class
internal sealed class TestGrainContext : IGrainContext
{
    /// <inheritdoc/>
    public ActivationId ActivationId { get; set; }

    /// <inheritdoc/>
    public IServiceProvider ActivationServices { get; set; } = default!;

    /// <inheritdoc/>
    public GrainAddress Address { get; set; } = default!;

    /// <inheritdoc/>
    public Task Deactivated { get; set; } = Task.CompletedTask;

    /// <inheritdoc/>
    public GrainId GrainId { get; set; }

    /// <inheritdoc/>
    public object GrainInstance { get; set; } = default!;

    /// <inheritdoc/>
    public GrainReference GrainReference { get; set; } = default!;

    /// <inheritdoc/>
    public IGrainLifecycle ObservableLifecycle { get; set; } = default!;

    /// <inheritdoc/>
    public IWorkItemScheduler Scheduler { get; set; } = default!;

    /// <inheritdoc/>
    public void Activate(Dictionary<string, object>? requestContext, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Grain activation is not supported in test scenarios.");

    /// <inheritdoc/>
    public void Deactivate(DeactivationReason deactivationReason, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Grain deactivation is not supported in test scenarios.");

    /// <inheritdoc/>
    public bool Equals(IGrainContext? other) => ReferenceEquals(this, other);

    /// <summary>
    /// Gets a component from the grain context.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <returns>The component instance.</returns>
    /// <exception cref="NotSupportedException">Always thrown for test scenarios.</exception>
    public TComponent GetComponent<TComponent>()
        where TComponent : class =>
        throw new NotSupportedException($"GetComponent<{typeof(TComponent).Name}> is not supported in test scenarios.");

    /// <inheritdoc/>
    public object? GetComponent(Type componentType) =>
        throw new NotSupportedException($"GetComponent({componentType.Name}) is not supported in test scenarios.");

    /// <summary>
    /// Gets a strongly typed grain target from the grain context.
    /// </summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The target instance.</returns>
    /// <exception cref="NotSupportedException">Always thrown for test scenarios.</exception>
    public TTarget GetTarget<TTarget>()
        where TTarget : class =>
        throw new NotSupportedException($"GetTarget<{typeof(TTarget).Name}> is not supported in test scenarios.");

    /// <inheritdoc/>
    public object? GetTarget() =>
        throw new NotSupportedException("GetTarget is not supported in test scenarios.");

    /// <inheritdoc/>
    public void Migrate(Dictionary<string, object>? requestContext, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Grain migration is not supported in test scenarios.");

    /// <inheritdoc/>
    public void ReceiveMessage(object message) =>
        throw new NotSupportedException("ReceiveMessage is not supported in test scenarios.");

    /// <inheritdoc/>
    public void Rehydrate(IRehydrationContext context) =>
        throw new NotSupportedException("Grain rehydration is not supported in test scenarios.");

    /// <inheritdoc/>
    public void SetComponent<TComponent>(TComponent? value)
        where TComponent : class =>
        throw new NotSupportedException($"SetComponent<{typeof(TComponent).Name}> is not supported in test scenarios.");
}
