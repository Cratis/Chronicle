---
name: cratis-showrunner-herald
description: Use when a Cratis .NET application sends email such as invitations or welcome messages with Herald (`Cratis.Showrunner.Herald`), including `AddHerald`, `IEmails`, email templates and images, SMTP or the development folder transport. Not for a custom SMTP client or for Ante's token issuance.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-showrunner-herald/SKILL.md -->

# Herald email

Herald sends the email an application needs from template and image files, and records every
email as a fact. It is a **library** that runs in the application's process and keeps its state
as events in the application's own Chronicle event store. It is not a service.

**Requires** `Cratis.Showrunner.Herald` 1.10.0 or later and an application that is a Chronicle
client (`AddCratis` or `AddCratisChronicle`); check the package reference in the `.csproj`.
Herald records in the host application's own event store, without naming it again; set
`HeraldOptions.EventStore` only to use another store. Showrunner 1.13.0 or later takes the host's
store; an older release takes the name from the Chronicle client options, which an ASP.NET Core
host leaves empty, so set `EventStore` explicitly there.

## Add it

```csharp
builder.AddCratis();
builder.AddHerald(herald =>
{
    herald.TemplatesFolder = Path.Combine(builder.Environment.ContentRootPath, "Emails");
    herald.ImagesFolder = Path.Combine(builder.Environment.ContentRootPath, "Emails", "images");
    herald.Sender = new() { Address = builder.Configuration["Herald:Sender"]!, Name = "Ledger" };
    herald.Smtp = new()
    {
        Host = builder.Configuration["Herald:SmtpHost"]!,
        Port = 587,
        Security = SmtpSecurity.StartTls,
        UserName = builder.Configuration["Herald:SmtpUserName"]!,
        Password = builder.Configuration["Herald:SmtpPassword"]!
    };
});
```

- **Take the sender, host, user name and password from configuration, never literals.** For a
  Screenplay application these are the `onboarding.email.*` environment values (see
  [cratis-environments-and-secrets](../cratis-environments-and-secrets/SKILL.md)), and the
  password is a secret that never enters source, a log, an event or the transcript.
- `TemplatesFolder` is required and must exist; `Sender.Address` is required. Options are
  validated at startup and the error names the option.
- Defaults worth knowing: `DefaultLanguage` `en`, `Transport` `Smtp`, `Smtp.Security` `StartTls`,
  `Retry.MaxAttempts` 6, `PollingInterval` 5 seconds. Read [options](references/options.md) for the full table.
- A `signupDeclined` email is sent from `OrganizationRegistrationDeclined` with the reviewer's message as a value; see `cratis-onboarding-runtime`.
- Chronicle finds Herald's events through type discovery of referenced `Cratis*` packages.
- Run the worker in **one** application instance: delivery is at least once.

## Send

```csharp
await emails.Send(new EmailRequest(
    Kind: "invitation",
    About: context.EventSourceId,
    To: @event.Email,
    Values: new Dictionary<string, string> { ["invitation.link"] = @event.Link })
{
    ToName = @event.Name,
    Language = @event.Language
});
```

Send from where the decision is made: a reactor on the fact that an invitation was issued, or an
Arc command. `Send` renders first and throws `EmailCannotBeRendered` without queuing anything when
a template, value or image is wrong; otherwise it appends `EmailQueued` against `About`, returns
the `EmailId` and does not wait for the server. Pass the compliance `Subject` so erasing the
person erases the recipient and any link that works as a credential. Report a bounce with
`emails.Bounced(about, emailId, kind, reason)` and leave the address out of the reason.

## Templates and images

A kind is lowercase letters, digits and dashes. The folder holds `{kind}.json` (the manifest of
accepted values) and `{kind}.{language}.html` per language. Read
[templates and images](references/templates-and-images.md) before writing either.

- Templates are strict Handlebars with only `{{value}}`, `{{#if value}}…{{else}}…{{/if}}` and `{{date value "format"}}`. Triple braces, partials and other helpers are refused.
- A value the manifest does not declare is an error, never an empty space. Every value is HTML-encoded.
- Images are PNG, JPEG or GIF referenced as `cid:name.png`. **SVG is refused**; export a PNG.

## Facts and delivery

`EmailSent`, `EmailFailed` and `EmailBounced` are in `Cratis.Showrunner.Herald.Contracts`, carry
the `EmailId`, and never the recipient or a value; project and react to them. They are not
`[Public]`: they live in the event log so the application's own projections can follow them.
`EmailQueued` and `EmailAttemptFailed` are Herald's own. A change to a released event is a new generation, not an edit.

For development set `Transport = EmailTransportKind.Folder` and `PickupFolder`; each email is
written as an `.eml` file and nothing leaves the machine. To send through a provider API, register
a custom `IEmailTransport` before `AddHerald`.

## Pitfalls

- Hard-coding the SMTP password in `AddHerald` or committing it: read it from configuration or the environment.
- Sending a new kind without its manifest, or with a value the manifest lacks: `EmailCannotBeRendered`.
- Putting a remote `https://` image in a template: use `cid:`.
- Running two worker instances: duplicate deliveries.

## Verify

1. `dotnet build` is clean and the host starts: option errors surface at startup naming the option.
2. With the `Folder` transport, send one email in a spec or locally and open the `.eml`: the subject, plain-text alternative and embedded image are present.
3. The event log has `EmailQueued` then `EmailSent` against the `About` id.

## Route near misses

- Events, reactors and projections that call Herald: `cratis-chronicle-reactor`, `cratis-chronicle-projection`.
- Invitation tokens, the lobby and acceptance: `cratis-onboarding-runtime`.
- Erasing personal data: `cratis-chronicle-compliance`.
