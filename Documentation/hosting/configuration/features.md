# Features

Feature toggles enable or disable specific Chronicle Server capabilities.

## Example configuration

```json
{
  "features": {
    "api": true,
    "workbench": true,
    "changesetStorage": false,
    "oAuthAuthority": true
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| api | boolean | true | Enable REST API endpoint |
| workbench | boolean | true | Enable web-based management interface |
| changesetStorage | boolean | false | Enable changeset storage functionality |
| oAuthAuthority | boolean | true | Enable internal OpenIdDict-based OAuth authority |

If the API is disabled, the Workbench is also disabled because it depends on the API. The OAuth authority is automatically disabled when an external authority is configured via `authentication.authority`.

