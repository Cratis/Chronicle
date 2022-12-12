# Identity

Cratis' Application Model provides a way to easily work with providing an object that represents properties the application finds important for describing
the logged in user. The purpose of this is to provide details about the logged in user on the ingress level of an application and letting it
provide the details on the request going in. Having it on the ingress level lets you expose the details to all microservices behind the ingress.

> Note: Aksio has an ingress middleware that uses this technique and takes the details and puts it on a cookie. For more details go [here](https://github.com/aksio-insurtech/IngressMiddleware).

The values provided by the provider are values that are typically application specific and goes beyond what is already found in the token representing the user.
This is optimized for working with Microsoft Azure well known HTTP headers passed on by the different app services, such as Azure ContainerApps or WebApps.
The following headers are required for it to be able to resolve:

| Header | Description |
| ------ | ----------- |
| x-ms-client-principal | The token holding all the details, base64 encoded JWT token |
| x-ms-client-principal-id | The unique identifier from the identity provider for the identity |
| x-ms-client-principal-name | The name of the identity, typically resolved from claims within the token |

To support the identity details, one of your microservices in your application can implement the `IProvideIdentityDetails` interface
found in the `Aksio.Cratis.ApplicationModel.Identity` namespace.

> Note: If your application has just one microservice, you let it implement the `IProvideIdentityDetails` interface.

Below is an example of an implementation:

```csharp
public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<object> Provide(IdentityProviderContext context)
    {
        object result = new { Hello = "World" };
        return Task.FromResult(result);
    }
}
´´´

> Note: Dependency inversion works for this, so your provider can take any dependencies it wants on its constructor.

Your provider will be exposed on a well known route: `/.aksio/me`.
