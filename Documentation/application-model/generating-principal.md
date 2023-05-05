# Generating principal

While working locally you still want to test the different identities and possibly claims that are put on the identity in combination.
The [backend part](./identity.md) of the identity system is relying on the following HTTP headers being set:

| Header | Description |
| ------ | ----------- |
| x-ms-client-principal | The token holding all the details, base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| x-ms-client-principal-id | The unique identifier from the identity provider for the identity |
| x-ms-client-principal-name | The name of the identity, typically resolved from claims within the token |

Once these are set and the `x-ms-client-principal` is in the expected format, it
will pass these onto your **identity details provider**.

The expected format needs to be according to the [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data).
To simulate users, all you have to do is generate the correct values and use an extension for your browser to set the
HTTP request headers.

```json
{
  "identityProvider": "aad",
  "userId": "e7f664ca-4ecc-45be-84cf-74b6240d049a",
  "userDetails": "jane@doe.io",
  "userRoles": ["anonymous", "authenticated"],
  "claims": [{
    "typ": "socialno",
    "val": "12345678901"
  }, {
    "typ": "surname",
    "val": "Doe"
  }, {
    "typ": "givenname",
    "val": "Jane"
  }]
}
```

Basically what you then need to do is generate something that matches that structure and `Base64` encode it.
If you're using VSCode, you could use an [extension](https://marketplace.visualstudio.com/items?itemName=adamhartford.vscode-base64) for doing the base 64 encoding.
As an alternative, you could also use an online base64 encoder like [this](https://www.base64encode.org).

For the above structure that would become:

```text
ewogICJpZGVudGl0eVByb3ZpZGVyIjogImFhZCIsCiAgInVzZXJJZCI6ICJlN2Y2NjRjYS00ZWNjLTQ1YmUtODRjZi03NGI2MjQwZDA0OWEiLAogICJ1c2VyRGV0YWlscyI6ICJqYW5lQGRvZS5pbyIsCiAgInVzZXJSb2xlcyI6IFsiYW5vbnltb3VzIiwgImF1dGhlbnRpY2F0ZWQiXSwKICAiY2xhaW1zIjogW3sKICAgICJ0eXAiOiAic29jaWFsbm8iLAogICAgInZhbCI6ICIxMjM0NTY3ODkwMSIKICB9LCB7CiAgICAidHlwIjogInN1cm5hbWUiLAogICAgInZhbCI6ICJEb2UiCiAgfSwgewogICAgInR5cCI6ICJnaXZlbm5hbWUiLAogICAgInZhbCI6ICJKYW5lIgogIH1dCn0K
```

From the terminal on a Unix based operating system you could also generate it:

```shell
echo "{\"claims\":[{\"typ\":\"socialno\",\"val\":\"1234568790\"}" |base64
```

Which would generate:

```text
eyJjbGFpbXMiOlt7InR5cCI6InNvY2lhbG5vIiwidmFsIjoiMTIzNDU2ODc5MCJ9Cg==
```

## ModHeader

Once you have the principal as **base64** you can put it directly as a header for the request.

In your browser you can use an extension such as [ModHeader](https://modheader.com). It allows you to setup headers
that can be added to the request. Use this to add the expected headers.
The `x-ms-client-principal-id` is often just a `Guid` or an identifier that the source identity provider identifies the
person with. While the `x-ms-client-principal-name` is often just an email address for the person.

> Important: For the `x-ms-client-principal` you want to paste the **base64** generated generated value and add a `=` at the end.
This will make sure the base64 string is valid.

![](./configure-mod-header.png)

> Pro-tip: With ModHeader you can create profiles. This is super useful if you want to be testing with different users and easily just switch between them.
