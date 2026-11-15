<!-- cratis-ai-managed: skills/cratis-showrunner-herald/references/options.md -->
# Herald options

| Option | Default | Meaning |
|---|---|---|
| `TemplatesFolder` | required | Folder with the templates; must exist. |
| `ImagesFolder` | none | Folder with the images; optional when no template has an image. |
| `DefaultLanguage` | `en` | Language used when no template exists in the language asked for. |
| `Sender.Address` | required | The address the emails come from. |
| `Sender.Name`, `Sender.ReplyTo` | empty | Display name; where replies go. |
| `Look.BrandColor` | `#2563eb` | Button and link color as `#rrggbb`; templates use `{{look.brandColor}}`. |
| `Look.Footer` | empty | Shared footer; templates use `{{look.footer}}`. |
| `Transport` | `Smtp` | `Smtp`, or `Folder` to write `.eml` files. |
| `Smtp.Host`, `Smtp.Port` | none, `587` | The mail server. |
| `Smtp.Security` | `StartTls` | `StartTls` (587), `Tls` (465) or `None` (a relay on the same machine or network). |
| `Smtp.UserName`, `Smtp.Password` | empty | Sign-in; leave both empty for an open relay. The password is a secret. |
| `PickupFolder` | none | Where the `Folder` transport writes. |
| `Retry.MaxAttempts` | `6` | Deliveries tried before the email fails for good. |
| `Retry.InitialDelay`, `Retry.MaxDelay` | 30 s, 1 h | Wait after the first failure, doubling up to the maximum. |
| `PollingInterval` | 5 s | How often queued emails that are due are looked for. |
| `Namespace` | default namespace | Event store namespace for the facts. |

The server certificate is always validated; there is no switch to turn it off.

## Failure handling

| What happened | Result |
|---|---|
| Server unreachable, sign-in refused, timeout | `EmailAttemptFailed`, retried with growing delay. A bad sign-in is retried because it can be fixed while the email waits. |
| Server refuses message or recipient for good (550 to 554) | `EmailFailed` at once. |
| Last allowed attempt fails | `EmailFailed` with the attempt count. |
| Template or image gone when due | `EmailFailed` with zero attempts. |

Failure reasons are sentences Herald wrote, with the password and recipient address removed. An
unexpected failure is recorded by its kind only. Which emails are waiting comes from the event
log; the `QueuedEmail` read model lists emails with no outcome yet, for a count.
