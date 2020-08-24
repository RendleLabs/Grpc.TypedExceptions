# Grpc.TypedExceptions

This package adds a generic `RpcException<T>` type to gRPC, and provides
an `Interceptor` that propagates the typed exception from server to
client, providing similar functionality to WCF's
[FaultException](https://docs.microsoft.com/en-us/dotnet/api/system.servicemodel.faultexception-1?view=dotnet-plat-ext-3.1).

## Using `RpcException<T>`

The `T` type parameter is constrained to Protobuf types; that is, types
that implement `Google.Protobuf.IMessage`. You will need to define error
messages in your `.proto` file to use with it.

```protobuf
message MyCustomError {
    string reason = 1;
}
```

In your server code, you can throw an exception like this:

```csharp
var error = new MyCustomError { Reason = "Bork bork bork" };
throw new RpcException<MyCustomError>(error);
```

You can catch this specific exception in your client code like this:

```csharp
try
{
    var response = await client.GetThing(request);
}
catch (RpcException<MyCustomError> ex)
{
    Console.WriteLine(ex.Detail.Reason);
}
catch (RpcException ex)
{
    // Handle other exceptions
}
```

This gives you more granular control over exception handling, as well
as more useful information included in the exceptions.

## Applying the `TypedExceptionInterceptor`

On the server, you can add the interceptor to all services in a gRPC
application using the options callback for `AddGrpc`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddGrpc(options =>
    {
        // Add the interceptor to all services
        options.Interceptors.Add<TypedExceptionInterceptor>();
    });
}
```

For the client, if you're creating it manually you need to use the
`Intercept` method on `GrpcChannel` to create a `CallInvoker` you
can then pass to the client constructor:

```csharp
var invoker = GrpcChannel.ForAddress(httpClient.BaseAddress, new GrpcChannelOptions
    {
        HttpClient = httpClient
    })
    .Intercept(new TypedExceptionInterceptor());

var client = new MyService.MyServiceClient(invoker);
```

If you're using the client factory in a .NET Core application, you
can add the interceptor when registering the service:

```csharp
services
    .AddGrpcClient<MyService.MyServiceClient>(o =>
    {
        o.Address = new Uri("https://localhost:5001");
    })
    .AddInterceptor(() => new TypedExceptionInterceptor());
```
