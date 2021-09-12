// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.DependencyInversion
{
    /// <summary>
    /// Delegate type that is automatically hooked up with the IoC container to provide a means to get
    /// instances of types when your lifetime scope is longer or very different than what you depend on.
    /// </summary>
    /// <typeparam name="T">Type to provide for.</typeparam>
    /// <returns>Instance of the type.</returns>
    /// <remarks>
    /// This can typically be very useful when you have something that is in a singleton per tenant scope
    /// and your service is in singleton. Putting this in between means you get to resolve it properly in
    /// the correct scope. Obviously you can setup a manually registration for these things with delegates,
    /// but the point of this is that this is automatically hooked up for you.
    /// Another good usecase for this is for constructors that has a dependency which is per tenant but
    /// execution context might not have been established at the time, or you're unsure if you can keep
    /// the instance around between calls. For instance with Orleans Grains, it is Orleans that control the
    /// lifecycle of the grain. This would then be a good place to use this.
    /// </remarks>
    public delegate T ProviderFor<T>();
}
