// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.BDD
{
    /// <summary>
    /// Represents the base class for specifications.
    /// </summary>
    /// <remarks>
    /// The lifecycle of a specifiction is as follows:
    /// - Establish : establishes the context (Given)
    /// - Because : performs the action       (When)
    /// - [Fact] : all your facts             (Then)
    /// - Destroy : performs cleanup of the context
    /// .
    /// The different methods are by convention, meaning that having a private method called "Establish", "Destroy", "Because"
    /// with a void signature taking no arguments will automatically be called.
    /// All "Then" statements are considered the xUnit Facts we want to run assertions for.
    /// .
    /// It will recursively execute lifecycle methods in the inheritance hierarchy.
    /// This enables one to encapsulate reusable contexts. The order it executes them in
    /// is reversed; meaning that it will start at the lowest level in the inheritance chain
    /// and move towards the specific type.
    /// Example:
    /// Context class:
    /// public class a_specific_context : Specification
    /// {
    ///     void Establish() => ....
    /// }
    /// .
    /// Specification class:
    /// _
    /// public class when_doing_something : a_specific_context
    /// {
    ///     void Establish() => ....
    ///     void Because() => ....
    ///     [Fact] It_should_do_something() => ....
    /// }
    /// .
    /// It will run the Establish first for the `a_specific_context` and then the `when_doing_something`
    /// class.
    /// </remarks>
    public class Specification : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Specification"/>.
        /// </summary>
        public Specification()
        {
            OnEstablish();
            OnBecause();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            OnDestroy();
            GC.SuppressFinalize(this);
        }

        void OnEstablish()
        {
            InvokeMethod("Establish");
        }

        void OnBecause()
        {
            InvokeMethod("Because");
        }

        void OnDestroy()
        {
            InvokeMethod("Destroy");
        }

        void InvokeMethod(string name)
        {
            typeof(SpecificationMethods<>)
                .MakeGenericType(GetType())
                .GetMethod(name, BindingFlags.Static | BindingFlags.Public)?
                .Invoke(null, new object[] { this });
        }
    }
}
