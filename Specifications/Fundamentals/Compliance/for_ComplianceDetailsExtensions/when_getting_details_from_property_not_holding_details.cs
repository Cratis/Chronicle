using System.Reflection;

namespace Cratis.Compliance.for_ComplianceDetailsExtensions
{
    public class when_getting_details_from_property_not_holding_details : Specification
    {

        class MyClass
        {
            public string Something { get; set; }

            public static PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
        }

        string result;

        void Because() => result = MyClass.SomethingProperty.GetComplianceMetadataDetails();

        [Fact] void should_return_empty_string() => result.ShouldEqual(string.Empty);
    }
}
