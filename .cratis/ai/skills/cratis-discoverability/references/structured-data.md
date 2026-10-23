<!-- cratis-ai-managed: skills/cratis-discoverability/references/structured-data.md -->
# Structured data patterns

## Contents

- Rules
- Types per page type
- Pattern

## Rules

- One `@graph` per page with stable `@id` values. The organization `@id` is
  `https://cratis.no/#organization`.
- Markup describes what is visible on the page, and every date is a real one.
- Add no `HowTo`. Add no new `FAQPage`; keep the visible question and answer text. Use `QAPage` only
  for real user questions and answers.
- Use only public, owner-approved founder/company facts that are visible on the page. Founder
  `Person` nodes may be linked from the organization with stable `@id`s and public profile links;
  do not infer roles, employment or relationships from contributor accounts. Without this approval,
  reference the organization by `@id` instead of naming a person.
- Never add personal data or `Person` entities for community members, even if a profile displays
  them. Do not serialize job titles, email, location, interests or CV details into JSON-LD.
  Member profile and CV pages must emit `noindex`; keep them out of the sitemap. Do not apply
  this policy to the community home or join page. Check rendered robots metadata or the
  `X-Robots-Tag` header; blocking crawling alone does not establish `noindex`.
- Shared guidance stays generic: no private/unannounced integration names, legal entity details
  or factual counts copied from source material.
- Do not add `offers`, an address or contact data without approval (see `SITE.md`).
- Validate with the schema.org validator or Google's Rich Results Test. Invalid markup is worse than
  none. Static sites hand-write `<script type="application/ld+json">`; Astro sites generate it in
  the head component, so change it there rather than per page.

## Types per page type

| Page type | Types |
| --- | --- |
| Company home | `Organization` (name, url, logo, `sameAs`), `WebSite` |
| Product marketing page | `SoftwareApplication` (name, applicationCategory, license, url, `sameAs`) |
| Docs concept, tutorial, how-to, reference | `TechArticle` with `headline`, `dateModified`, plus `BreadcrumbList` |
| Docs hub and landing page | `CollectionPage` or `WebPage`, not `TechArticle` |
| Blog post | `BlogPosting` (`datePublished`, `dateModified`, `publisher`, `image`) |
| Repository or sample code | `SoftwareSourceCode` |
| Legal pages | `WebPage` |
| Member profiles and CV pages | no `Person` or personal structured data; `noindex` |
| 404 and app hosts | none |

## Pattern

```json
{
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "TechArticle",
      "@id": "https://www.cratis.io/chronicle/projections/#article",
      "headline": "Chronicle projections",
      "description": "A projection declares how Chronicle builds a read model from events.",
      "dateModified": "YYYY-MM-DD",
      "publisher": { "@id": "https://cratis.no/#organization" },
      "mainEntityOfPage": "https://www.cratis.io/chronicle/projections/"
    },
    {
      "@type": "BreadcrumbList",
      "itemListElement": [
        { "@type": "ListItem", "position": 1, "name": "Chronicle", "item": "https://www.cratis.io/chronicle/" },
        { "@type": "ListItem", "position": 2, "name": "Projections" }
      ]
    }
  ]
}
```

The values are illustrative: take the title, description and dates from the page itself.
