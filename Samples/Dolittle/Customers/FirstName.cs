using Cratis.Compliance;

namespace Dolittle.Customers
{
    [ComplianceDetails("Needs special handling")]
    public record FirstName(string Value) : PIIConceptAs<string>(Value);
}
