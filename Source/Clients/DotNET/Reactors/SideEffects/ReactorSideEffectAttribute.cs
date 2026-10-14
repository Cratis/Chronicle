// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Marks a type as a reactor side-effect return type for the analyzer.
/// </summary>
/// <remarks>
/// This attribute only informs the analyzer; it does not register a handler or enable runtime processing.
/// A registered <see cref="IReactorSideEffectHandler"/> must still claim the declared synchronous return type
/// and process the returned value, including any collection of side effects.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true)]
public sealed class ReactorSideEffectAttribute : Attribute;
