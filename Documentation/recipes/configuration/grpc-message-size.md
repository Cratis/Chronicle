# gRPC Message Size Configuration

When working with large event batches or queries that return many events, you may encounter gRPC message size limits. Chronicle provides configuration options to adjust the maximum send and receive message sizes.

## Default Settings

By default, Chronicle sets both `MaxReceiveMessageSize` and `MaxSendMessageSize` to **100 MB** (104,857,600 bytes). This is significantly higher than the default gRPC limit of 4 MB, making it suitable for most scenarios involving large event collections.

## Configuring Message Sizes

You can configure the message sizes when creating a `ChronicleClient` or through options:

```csharp
var options = new ChronicleOptions
{
    Url = new ChronicleConnectionString("chronicle://localhost:35000"),
    MaxReceiveMessageSize = 200 * 1024 * 1024, // 200 MB
    MaxSendMessageSize = 200 * 1024 * 1024      // 200 MB
};

var client = new ChronicleClient(options);
```

## Configuration via appsettings.json

For ASP.NET Core applications, you can configure message sizes in your `appsettings.json`:

```json
{
  "Chronicle": {
    "Url": "chronicle://localhost:35000",
    "MaxReceiveMessageSize": 209715200,
    "MaxSendMessageSize": 209715200
  }
}
```

## Configuration via Environment Variables

Message sizes can also be set using environment variables:

```bash
Chronicle__MaxReceiveMessageSize=209715200
Chronicle__MaxSendMessageSize=209715200
```

## Understanding the Error

If you encounter an error like:

```
Status(StatusCode="ResourceExhausted", Detail="Received message exceeds the maximum configured message size.")
```

This indicates that the response from the server exceeds the `MaxReceiveMessageSize`. You should increase this value to accommodate larger responses.

## Recommendations

- **Default (100 MB)**: Suitable for most applications with moderate event batch sizes
- **200+ MB**: Consider for applications that frequently query large event ranges or work with many events
- **Performance Considerations**: Larger message sizes consume more memory. Balance your needs with available resources
- **Server Configuration**: Ensure the Chronicle Kernel server is also configured to handle the message sizes you need

## Null Values

Setting either property to `null` will use the gRPC default (4 MB), which is generally **not recommended** for Chronicle applications:

```csharp
var options = new ChronicleOptions
{
    MaxReceiveMessageSize = null, // Uses gRPC default of 4 MB - not recommended
    MaxSendMessageSize = null     // Uses gRPC default of 4 MB - not recommended
};
```
