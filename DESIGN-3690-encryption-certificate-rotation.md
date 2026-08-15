# 3690 — Encryption-certificate rotation: the mechanism

This note records the mechanism chosen for [#3690](https://github.com/Cratis/Chronicle/issues/3690) before
it was implemented, what it deliberately does not cover, and what an operator has to do and in what order.
The operator-facing version of the same material lives in `Documentation/hosting/encryption-certificate.md`;
this note is the design record.

## The question that had to be answered first

`EncryptionCertificate` was one path and one password. The single protection call was
`ProtectKeysWithCertificate(certificate)`, and `UnprotectKeysWithAnyCertificate` appeared nowhere in the
repository. Three consumers load that same file:

| Consumer | What it protects | Who owns the format |
| --- | --- | --- |
| ASP.NET Core Data Protection | The Data Protection key ring in Chronicle's storage — which in turn protects OAuth tokens | ASP.NET Core |
| OpenIddict | The internal authority's token encryption and signing credentials | OpenIddict |
| `IEncryption` | Webhook credentials (basic password, bearer token, OAuth client secret) | Chronicle |

Replacing the file made every previously protected value undecryptable, with no overlap window.

The alternative was a documented re-encryption window with downtime: stop every node, decrypt everything
with the old certificate, re-encrypt with the new one, start again. That was rejected. Two of the three
consumers are libraries whose stored format Chronicle does not own, so a re-encryption pass would have to
rewrite the Data Protection key ring and OpenIddict's stored tokens through APIs neither library exposes for
that purpose. It also fails the thing rotation exists for: a certificate that has to be replaced *now*
because it leaked cannot wait for a maintenance window.

**Chosen: an additive, ordered key ring.** One certificate is active; zero or more previous certificates
stay loaded for decryption only. Every consumer already supports exactly this shape, which is why it costs
no format change:

- Data Protection: `ProtectKeysWithCertificate(active)` plus `UnprotectKeysWithAnyCertificate(all)`.
- OpenIddict: `AddEncryptionCertificates(all)` / `AddSigningCertificates(all)` — its own documentation says
  "multiple credentials can be added to support key rollover".
- `IEncryption`: Chronicle's own format, extended below.

## Configuration

`EncryptionCertificate` keeps `certificatePath` / `certificatePassword` as the **active** certificate and
gains an ordered `previous` list. An existing configuration binds unchanged, with an empty ring tail.

```json
{
  "encryptionCertificate": {
    "certificatePath": "/certs/encryption-2026.pfx",
    "certificatePassword": "…",
    "previous": [
      { "certificatePath": "/certs/encryption-2025.pfx", "certificatePassword": "…" }
    ]
  }
}
```

Environment variables index the list the way the configuration binder always does:
`Cratis__Chronicle__EncryptionCertificate__Previous__0__CertificatePath`.

The ring is materialized once per process and held for its lifetime. Adding, promoting or retiring a
certificate is a restart, not a reload — the same as every other certificate Chronicle loads.

## Key identity, and why ciphertext is labeled

The key id is the certificate's **thumbprint**. It is the identifier Data Protection and OpenIddict already
key on, it is public (it travels in the clear inside the Data Protection key ring and in every TLS
handshake), and it is stable across re-exports of the same key pair.

`IEncryption` ciphertext was bare base64 of an RSA-OAEP block with nothing identifying the key. Two things
follow from that: after a rotation the only way to read a value is to guess which certificate made it, and
a value made by a certificate that has since been retired is indistinguishable from a corrupt one. So the
envelope now carries the key id:

```text
crk1:<thumbprint>:<base64 RSA-OAEP-SHA256 ciphertext>
```

`crk1` is the format marker; `:` never occurs in base64, so parsing is unambiguous. Values written before
this change carry no marker and are still read: they are tried against each certificate in ring order,
active first. A labeled value whose key id is not in the ring raises `EncryptionCertificateNotInRing`,
which names the required key id and the key ids that are loaded — and never the ciphertext.

### The cost of the label

A value written by a node running this version cannot be read by a node running an older one; the older
code hands the whole string to `Convert.FromBase64String`. This only matters inside a rolling upgrade, and
only for webhook credentials created or changed during it. Finish the rolling upgrade before creating or
changing webhook credentials, and before rotating. This is stated in the hosting documentation.

## Fail closed

The ring refuses to start rather than degrade quietly. Every one of these was previously either silent or
reported as something else:

| Configuration | Before | Now |
| --- | --- | --- |
| `certificatePath` set, file absent | Data Protection wrote its keys unencrypted; OpenIddict reported "an encryption certificate is required in production" | `EncryptionCertificateFileNotFound`, naming the path |
| A `previous` entry's file absent | n/a — there was no `previous` | `EncryptionCertificateFileNotFound`, naming the path |
| A `previous` entry with no `certificatePath` | n/a | `EncryptionCertificateWithoutPath` |
| `previous` set, no active certificate | n/a | `PreviousEncryptionCertificatesWithoutActive` |
| The same certificate active and previous | n/a | `DuplicateEncryptionCertificateInRing` — an overlap that is not one |
| A certificate with no private key | An opaque OpenIddict failure, or a lazy `MissingPrivateKeyInCertificate` | `EncryptionCertificateWithoutPrivateKey`, naming the key id and path |

Nothing changes when no certificate is configured at all: development still generates one lazily for value
encryption, and production still raises `EncryptionCertificateNotConfigured` on first use.

## The rotation diagnostic

`GET /diagnostics/encryption-certificates` (authenticated, like every endpoint that is not explicitly
anonymous) answers the three questions an operator has during a rotation:

1. **What is in the ring, and which one is active** — from the ring itself: key id, role, subject, validity
   window, whether it has expired, and the path it was loaded from.
2. **Does anything still depend on a previous certificate** — by reading the stored Data Protection key
   ring and attributing each key to the certificate it is encrypted to. Each stored key carries the full
   DER of that certificate inside its `<X509Certificate>` element, so this is a fact, not an estimate.
3. **Has anything already been orphaned** — keys attributed to a certificate that is in neither the active
   nor the previous position come back with the role `Retired`. Those keys are already unreadable, which is
   what a rotation done in the wrong order looks like.

Keys that no certificate can be attributed to are counted separately: that is the "Data Protection wrote
its keys unencrypted" state, which nothing reported before.

The ring's state is also logged at startup, one line per certificate, and `IEncryption` logs a warning the
first time each previous certificate is used to read a value — the moment a dependency on it is observed.

## What this does not protect against

- **It does not re-encrypt anything.** A previous certificate stays required until every value it protects
  has been rewritten by ordinary traffic. The diagnostic tells you when that has happened for the Data
  Protection key ring; for webhook credentials it tells you only what has been *read* since the process
  started, because nothing enumerates them.
- **OpenIddict picks its own active credential.** Its documented ordering prefers the X.509 key with the
  furthest expiration date, not the one Chronicle marks active. In a normal rotation the new certificate
  outlives the old one and the two agree. Promote a certificate that expires *earlier* than one still in
  the ring and OpenIddict will keep issuing under the longer-lived one. Chronicle's own value encryption
  and the Data Protection key ring always follow the configured active certificate.
- **It does not protect a certificate you no longer have.** The ring makes an overlap possible; it does not
  make a lost private key recoverable. The backup ordering is the mitigation, and it is documented.
- **It is not a compliance-key mechanism.** PII keys under `compliance.encryption.storage` are a separate
  subsystem with its own lifecycle; the encryption certificate never protects them.
- **It does not detect a certificate that was replaced in place.** Overwriting the file at the configured
  path with a different key pair and restarting is still a destructive rotation with no overlap. The ring
  only helps if the previous file is kept and listed.

## Operator order

Rotation, forward only — every step is a restart of every node, and the ring must be identical on all of
them:

1. Issue the new certificate. Keep the old file.
2. Put the new one in `certificatePath` and move the old one to `previous[0]`. Restart every node.
3. Read `GET /diagnostics/encryption-certificates` until no key is attributed to the previous certificate.
   New Data Protection keys are created on the ring's own schedule; forcing it is not necessary and not
   supported.
4. Remove the entry from `previous`. Restart every node. Keep the file in the backup set until the last
   backup that predates step 2 has aged out.

Restore, which is the order that loses data when it is wrong:

1. Restore the certificate ring **first**, in the shape it had when the backup was taken — including every
   `previous` entry that was live then.
2. Restore the storage (Mongo or SQL) that holds the Data Protection key ring and the webhook definitions.
3. Restore the compliance key store if one is configured, to the same point in time as the storage.
4. Start one node and read the diagnostic before starting the rest. A restore into the wrong ring shows up
   as keys with the role `Retired`, before anything tries to serve traffic with them.

The failure mode this exists to prevent: a backup taken before a rotation contains data encrypted under a
certificate that has since been retired from the ring. Restoring it into the current ring makes it
unreadable, permanently, and nothing about the restore itself reports that. A certificate therefore has to
stay in the backup set for at least as long as the oldest restorable backup, which is longer than it has to
stay in the ring.
