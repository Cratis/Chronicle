// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;

namespace TestApp;

/// <summary>
/// Read model shape for the <see cref="EmployeeListProjection"/> declarative artifact.
/// </summary>
/// <param name="FirstName">The employee's first name.</param>
/// <param name="LastName">The employee's last name.</param>
/// <param name="Title">The employee's current job title.</param>
/// <param name="Address">The employee's street address.</param>
/// <param name="City">The employee's city.</param>
/// <param name="ZipCode">The employee's postal/zip code.</param>
/// <param name="Country">The employee's country.</param>
public record Employee(
    string FirstName = "",
    string LastName = "",
    string Title = "",
    string Address = "",
    string City = "",
    string ZipCode = "",
    string Country = "");

/// <summary>
/// Declarative projection that builds an <see cref="Employee"/> list view from
/// employee lifecycle events using the fluent projection builder.
/// </summary>
public class EmployeeListProjection : IProjectionFor<Employee>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<Employee> builder) => builder
        .From<EmployeeHired>()
        .From<EmployeeAddressSet>()
        .From<EmployeePromoted>(fb => fb
            .Set(m => m.Title).To(e => e.NewTitle))
        .From<EmployeeMoved>();
}

/// <summary>
/// Model-bound projection artifact for an employee detail view.
/// </summary>
/// <remarks>
/// <see cref="FromEventAttribute{TEvent}"/> on the class maps all matching property
/// names automatically. <see cref="SetFromAttribute{TEvent}"/> overrides the mapping
/// for properties whose name differs between the event and the model.
/// </remarks>
/// <param name="FirstName">The employee's first name.</param>
/// <param name="LastName">The employee's last name.</param>
/// <param name="Title">The employee's current job title.</param>
/// <param name="Address">The employee's street address.</param>
/// <param name="City">The employee's city.</param>
/// <param name="ZipCode">The employee's postal/zip code.</param>
/// <param name="Country">The employee's country.</param>
[FromEvent<EmployeeHired>]
[FromEvent<EmployeeAddressSet>]
[FromEvent<EmployeePromoted>]
[FromEvent<EmployeeMoved>]
public record EmployeeDetails(
    string FirstName = "",
    string LastName = "",
    [property: SetFrom<EmployeePromoted>(nameof(EmployeePromoted.NewTitle))]
    string Title = "",
    string Address = "",
    string City = "",
    string ZipCode = "",
    string Country = "");
