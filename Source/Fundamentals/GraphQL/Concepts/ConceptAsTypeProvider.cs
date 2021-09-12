// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cratis.Concepts;
using HotChocolate.Utilities;

namespace Cratis.GraphQL.Concepts
{
    public class ConceptAsTypeProvider<TConcept> : IChangeTypeProvider
    {
        public bool TryCreateConverter(Type source, Type target, ChangeTypeProvider root, [NotNullWhen(true)] out ChangeType converter)
        {
            if (source == typeof(TConcept))
            {
                var conceptValueType = source.GetConceptValueType();
                if (target == conceptValueType)
                {
                    converter = (value) => value!.GetConceptValue();
                    return true;
                }
            }

            if (target == typeof(TConcept))
            {
                var conceptValueType = target.GetConceptValueType();
                if (source == conceptValueType)
                {
                    converter = (value) => ConceptFactory.CreateConceptInstance(typeof(TConcept), value);
                    return true;
                }

                converter = (value) => ConceptFactory.CreateConceptInstance(typeof(TConcept), Convert.ChangeType(value, conceptValueType, CultureInfo.InvariantCulture));
                return true;
            }

            converter = null!;
            return false;
        }
    }
}
