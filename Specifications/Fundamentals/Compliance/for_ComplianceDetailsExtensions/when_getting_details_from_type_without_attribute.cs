namespace Cratis.Compliance.for_ComplianceDetailsExtensions
{
    public class when_getting_details_from_type_without_attribute : Specification
    {
        string result;

        void Because() => result = typeof(object).GetComplianceMetadataDetails();

        [Fact] void should_return_empty_string() => result.ShouldEqual(string.Empty);
    }
}
