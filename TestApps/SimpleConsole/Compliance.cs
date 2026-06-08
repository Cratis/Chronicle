// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Concepts;

namespace TestApp;

// ---------------------------------------------------------------------------
// PII-typed ConceptAs wrappers
// ---------------------------------------------------------------------------
// Marking the ConceptAs type with [PII] causes the Chronicle Kernel to
// automatically encrypt any property of that type at rest, in both events
// and read models — no per-property annotations are needed at the call site.

/// <summary>
/// Customer email address — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying email address string.</param>
[PII("Customer email address")]
public record CustomerEmail(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerEmail NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerEmail"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerEmail value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerEmail"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerEmail(string value) => new(value);
}

/// <summary>
/// Customer full legal name — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying full name string.</param>
[PII("Customer full legal name")]
public record CustomerFullName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerFullName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerFullName"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerFullName value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerFullName"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerFullName(string value) => new(value);
}

/// <summary>
/// Customer phone number — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying phone number string.</param>
[PII("Customer phone contact number")]
public record CustomerPhoneNumber(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerPhoneNumber NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerPhoneNumber"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerPhoneNumber value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerPhoneNumber"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerPhoneNumber(string value) => new(value);
}

/// <summary>
/// Customer street address — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying street address string.</param>
[PII("Customer street address")]
public record CustomerStreetAddress(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerStreetAddress NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerStreetAddress"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerStreetAddress value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerStreetAddress"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerStreetAddress(string value) => new(value);
}

/// <summary>
/// Customer city of residence — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying city name string.</param>
[PII("City of residence")]
public record CustomerCity(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerCity NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerCity"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerCity value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerCity"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerCity(string value) => new(value);
}

/// <summary>
/// Customer postal code — marked as PII at the type level.
/// </summary>
/// <param name="Value">The underlying postal code string.</param>
[PII("Postal code")]
public record CustomerPostalCode(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the not-set sentinel value.</summary>
    public static readonly CustomerPostalCode NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerPostalCode"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The concept value to convert.</param>
    public static implicit operator string(CustomerPostalCode value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerPostalCode"/>.
    /// </summary>
    /// <param name="value">The raw string value to wrap.</param>
    public static implicit operator CustomerPostalCode(string value) => new(value);
}

// ---------------------------------------------------------------------------
// Events
// ---------------------------------------------------------------------------
// The PII ConceptAs types carry their compliance metadata automatically — the
// Chronicle Kernel encrypts matching properties without any additional
// annotations at the event level.

/// <summary>
/// A customer has been registered.
/// </summary>
/// <param name="CustomerId">The unique customer identifier.</param>
/// <param name="Email">The customer's primary email address.</param>
/// <param name="FullName">The customer's full legal name.</param>
/// <param name="PhoneNumber">The customer's primary phone number.</param>
[EventType]
public record CustomerRegistered(
    string CustomerId,
    CustomerEmail Email,
    CustomerFullName FullName,
    CustomerPhoneNumber PhoneNumber);

/// <summary>
/// A customer's address has been updated.
/// </summary>
/// <param name="CustomerId">The unique customer identifier.</param>
/// <param name="StreetAddress">The new street address.</param>
/// <param name="City">The new city.</param>
/// <param name="PostalCode">The new postal code.</param>
/// <param name="Country">The new country.</param>
[EventType]
public record CustomerAddressUpdated(
    string CustomerId,
    CustomerStreetAddress StreetAddress,
    CustomerCity City,
    CustomerPostalCode PostalCode,
    string Country);

// ---------------------------------------------------------------------------
// Read model
// ---------------------------------------------------------------------------

/// <summary>
/// Customer read model demonstrating PII compliance with ConceptAs-typed properties.
/// </summary>
/// <remarks>
/// The Chronicle Kernel automatically encrypts properties whose type is annotated
/// with <see cref="PIIAttribute"/>. Country, CustomerNumber, AccountStatus, and
/// TotalOrders are plain types and are stored in clear text.
/// </remarks>
public record Customer
{
    /// <summary>Gets the customer identifier — not PII, used as the encryption subject.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the customer full name — automatically PII via <see cref="CustomerFullName"/>.</summary>
    public CustomerFullName FullName { get; init; } = CustomerFullName.NotSet;

    /// <summary>Gets the primary contact email — automatically PII via <see cref="CustomerEmail"/>.</summary>
    public CustomerEmail Email { get; init; } = CustomerEmail.NotSet;

    /// <summary>Gets the primary phone number — automatically PII via <see cref="CustomerPhoneNumber"/>.</summary>
    public CustomerPhoneNumber PhoneNumber { get; init; } = CustomerPhoneNumber.NotSet;

    /// <summary>Gets the street address — automatically PII via <see cref="CustomerStreetAddress"/>.</summary>
    public CustomerStreetAddress StreetAddress { get; init; } = CustomerStreetAddress.NotSet;

    /// <summary>Gets the city — automatically PII via <see cref="CustomerCity"/>.</summary>
    public CustomerCity City { get; init; } = CustomerCity.NotSet;

    /// <summary>Gets the postal code — automatically PII via <see cref="CustomerPostalCode"/>.</summary>
    public CustomerPostalCode PostalCode { get; init; } = CustomerPostalCode.NotSet;

    /// <summary>Gets the country — NOT PII, stored in clear text.</summary>
    public string Country { get; init; } = string.Empty;

    /// <summary>Gets the internal customer number — NOT PII.</summary>
    public string CustomerNumber { get; init; } = string.Empty;

    /// <summary>Gets the account status — NOT PII.</summary>
    public string AccountStatus { get; init; } = "active";

    /// <summary>Gets the total order count — NOT PII (anonymized metric).</summary>
    public int TotalOrders { get; init; }
}

// ---------------------------------------------------------------------------
// Static helpers — sample data and demo functions
// ---------------------------------------------------------------------------

/// <summary>
/// A fixed customer whose PII-typed events flow through the encrypted <see cref="Customer"/> read model.
/// </summary>
public static class SampleCustomer
{
    /// <summary>Gets the customer identifier.</summary>
    public const string Id = "c0000001-0000-0000-0000-000000000000";

    /// <summary>Gets the country.</summary>
    public const string Country = "USA";

    /// <summary>Gets the full name.</summary>
    public static readonly CustomerFullName FullName = "Eve Jackson";

    /// <summary>Gets the email.</summary>
    public static readonly CustomerEmail Email = "eve.jackson@example.com";

    /// <summary>Gets the phone number.</summary>
    public static readonly CustomerPhoneNumber PhoneNumber = "+1-202-555-0143";

    /// <summary>Gets the street address.</summary>
    public static readonly CustomerStreetAddress StreetAddress = "742 Evergreen Terrace";

    /// <summary>Gets the city.</summary>
    public static readonly CustomerCity City = "Springfield";

    /// <summary>Gets the postal code.</summary>
    public static readonly CustomerPostalCode PostalCode = "49007";
}

/// <summary>
/// Demo helpers for the compliance (PII) keyboard commands in the console sample.
/// </summary>
public static class ComplianceDemo
{
    /// <summary>
    /// Appends a sequence of PII-carrying events for <see cref="SampleCustomer"/>.
    /// </summary>
    /// <param name="store">The event store to append to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task RegisterCustomerWithPii(IEventStore store)
    {
        var registered = new CustomerRegistered(
            SampleCustomer.Id,
            SampleCustomer.Email,
            SampleCustomer.FullName,
            SampleCustomer.PhoneNumber);

        var addressUpdated = new CustomerAddressUpdated(
            SampleCustomer.Id,
            SampleCustomer.StreetAddress,
            SampleCustomer.City,
            SampleCustomer.PostalCode,
            SampleCustomer.Country);

        var result = await store.EventLog.AppendMany(SampleCustomer.Id, [registered, addressUpdated]);

        if (!result.IsSuccess)
        {
            var violations = string.Join("; ", result.ConstraintViolations.Select(v => v.Message));
            Console.WriteLine($"[pii] Could not register {(string)SampleCustomer.FullName}: {violations}");

            return;
        }

        var lastSequence = result.SequenceNumbers.Last();
        Console.WriteLine($"[pii] Registered {(string)SampleCustomer.FullName} ({SampleCustomer.Id}) with PII events up to sequence {lastSequence}");
    }

    /// <summary>
    /// Reads the <see cref="Customer"/> read model for <see cref="SampleCustomer"/> and prints it.
    /// </summary>
    /// <param name="store">The event store to read from.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ShowCustomerReadModel(IEventStore store)
    {
        var customer = await store.ReadModels.GetInstanceById<Customer>(SampleCustomer.Id);

        if (string.IsNullOrEmpty(customer.Id))
        {
            Console.WriteLine($"[pii] No Customer read model found for {SampleCustomer.Id}. Append the PII events first (press C).");

            return;
        }

        Console.WriteLine($"Customer read model for {customer.Id}:");
        Console.WriteLine($"  Full name      : {(string)customer.FullName}   [PII]");
        Console.WriteLine($"  Email          : {(string)customer.Email}   [PII]");
        Console.WriteLine($"  Phone number   : {(string)customer.PhoneNumber}   [PII]");
        Console.WriteLine($"  Street address : {(string)customer.StreetAddress}   [PII]");
        Console.WriteLine($"  City           : {(string)customer.City}   [PII]");
        Console.WriteLine($"  Postal code    : {(string)customer.PostalCode}   [PII]");
        Console.WriteLine($"  Country        : {customer.Country}");
        Console.WriteLine($"  Customer number: {customer.CustomerNumber}");
        Console.WriteLine($"  Account status : {customer.AccountStatus}");
        Console.WriteLine($"  Total orders   : {customer.TotalOrders}");
        Console.WriteLine("  PII fields are stored encrypted at rest — values above are the encrypted form.");
    }
}

// ---------------------------------------------------------------------------
// Reducer
// ---------------------------------------------------------------------------

/// <summary>
/// Folds customer lifecycle events into the <see cref="Customer"/> read model.
/// </summary>
public class CustomerReducer : IReducerFor<Customer>
{
    /// <summary>
    /// Handles a <see cref="CustomerRegistered"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">Unused — customer is always new on registration.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The new <see cref="Customer"/> state.</returns>
    public Task<Customer?> CustomerRegistered(CustomerRegistered @event, Customer? current, EventContext context)
    {
        var customer = new Customer
        {
            Id = @event.CustomerId,
            FullName = @event.FullName,
            Email = @event.Email,
            PhoneNumber = @event.PhoneNumber,
            CustomerNumber = $"CUST-{@event.CustomerId[..8]}"
        };

        return Task.FromResult<Customer?>(customer);
    }

    /// <summary>
    /// Handles a <see cref="CustomerAddressUpdated"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated <see cref="Customer"/> state.</returns>
    public Task<Customer?> CustomerAddressUpdated(CustomerAddressUpdated @event, Customer? current, EventContext context)
    {
        if (current is null)
        {
            return Task.FromResult<Customer?>(null);
        }

        return Task.FromResult<Customer?>(current with
        {
            StreetAddress = @event.StreetAddress,
            City = @event.City,
            PostalCode = @event.PostalCode,
            Country = @event.Country
        });
    }
}
