<!-- cratis-ai-managed: skills/cratis-showrunner-herald/references/templates-and-images.md -->
# Templates and images

## Layout

```
Emails/
  invitation.json          the values the invitation email accepts
  invitation.en.html       English
  invitation.nb.html       Norwegian
  images/
    logo.png
```

## Manifest

```json
{
  "values": [
    { "name": "invitation.link", "required": true },
    "invitation.expiresAt",
    "inviter.name"
  ]
}
```

A plain string is an optional value. A required value that is missing or blank stops the email
before it is queued. A name cannot be both a value and a group (`inviter` and `inviter.name`),
and `look` is reserved for `look.brandColor` and `look.footer`.

## Template

A header between two `---` lines carries `subject` and optionally `preview`; both may use values.
Then email-ready HTML.

```html
---
subject: {{inviter.name}} invited you to Ledger
preview: Accept the invitation before {{date invitation.expiresAt "d MMMM"}}
---
<html>
<head><style>.button { background-color: {{look.brandColor}}; color: #ffffff; }</style></head>
<body>
  <table role="presentation" width="600" align="center"><tr><td>
    <img src="cid:logo.png" alt="Ledger" width="120">
    {{#if inviter.name}}<p>{{inviter.name}} invited you.</p>{{/if}}
    <a class="button" href="{{invitation.link}}">Accept the invitation</a>
    <p>{{look.footer}}</p>
  </td></tr></table>
</body>
</html>
```

Author for email clients: table layout, about 600 px wide, `role="presentation"` on layout tables,
an `alt` on every image. Write styles in a `<style>` element; Herald moves the rules into `style`
attributes at render. Media queries stay in the element.

## Rendering rules

- Every value is HTML-encoded in the body, quotes in attributes included. Subject and preview are plain text and not encoded.
- The subject is collapsed to one line. The plain-text alternative is generated from the HTML.
- Dates are ISO 8601 and formatted in the culture of the email's language.
- Language falls back from `nb-NO` to `nb` to `DefaultLanguage`; the language used is recorded.

## Images

Referenced as `cid:logo.png` and embedded inline, so nothing is hosted and no remote content has
to be allowed. PNG, JPEG and GIF are embedded. SVG and anything else fail the rendering with an
error naming the file, because mail clients do not render SVG; export a PNG at twice the display
size. A referenced image that is missing also fails rendering. File names use letters, digits,
dots, dashes and underscores and are looked up only inside the images folder.
