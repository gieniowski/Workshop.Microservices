# Exercise 2: Publish-Subscribe

**Important: Before attempting the exercise, please ensure you have followed [the instructions for preparing your machine](README.md#preparing-your-machine-for-the-workshop) and that you have read [the instructions for running the exercise solutions](/README.md#running-the-exercise-solutions).**

NServiceBus endpoints communicate by sending each other messages. In this exercise, we'll focus on a specific type of messages: events. We will also explore a very powerful and popular messaging pattern: Publish-Subscribe (Pub-Sub).

Events are used to communicate that some action has taken place. They're informing us of a fact that something occurred in the past. In Pub-Sub, the sender (called Publisher) and the receiver (called Subscriber) are loosely coupled. There might be zero, one or multiple Subscribers interested in a specific event. In order to receive that event, they need to explicitly subscribe to it. In NServiceBus a subscription request is handled by the framework, together with message mappings and implementions of handlers which will process the event. The Publisher sends a copy of the event message to each subscriber.

## Overview

In the last exercise you extended the UI by showing additional information. For this exercise, the Orders page has a new button, "Create new order". In this exercise, we'll complete the process of placing a new order.

## Start-up projects

For more info, please see [the instructions for running the exercise solutions](/README.md#running-the-exercise-solutions).

* Divergent.Customers
* Divergent.Customers.API
* Divergent.Finance
* Divergent.Finance.API
* Divergent.Frontend
* Divergent.Sales
* Divergent.Sales.API
* Divergent.Shipping
* PaymentProviders

## Business requirements

When a customer creates a new order, that information is stored in a database. In order to complete the process, you need to provide the ability to pay for the placed order.

## What's provided for you

- Have a look at the `EndpointConfig` class in the `Divergent.Finance` project. Note that we use conventions to specify which messages are events:

  ```c#
  conventions.DefiningEventsAs(t =>
      t.Namespace != null &&
      t.Namespace.StartsWith("Divergent") &&
      t.Namespace.EndsWith("Events") &&
      t.Name.EndsWith("Event"));`
  ```

  If you create a class inside a namespace ending with "Events", and the name of this class ends with "Event", then NServiceBus will know it's an event.

- In the `Divergent.Finance/PaymentClient` directory you'll find a provided implementation for handling payments, named `ReliablePaymentClient`.


## Exercise 2.1: create and publish an `OrderSubmittedEvent`

In this exercise you'll create a new event named `OrderSubmittedEvent`. This event will be published by `SubmitOrderHandler` in the `Divergent.Sales` project.

### Step 1

Have a look at the following classes: `Customer` in `Divergent.Customers.Data.Models`, `Order` and `Product` in `Divergent.Sales.Data.Models`.

### Step 2

In the `Divergent.Sales.Messages` project, in the directory `Events`, add a new class called `OrderSubmittedEvent.cs`. The class should have three properties with public setters and getters: Id of the order, Id of the customer, and the list of product Ids.

```c#
namespace Divergent.Sales.Messages.Events
{
    public class OrderSubmittedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }

        public List<int> Products { get; set; }
    }
}
```

### Step 3

Have a look at `SubmitOrderHandler` class in the `Divergent.Sales` project. At the end of the `Handle` method, publish the `OrderSubmittedEvent`, by calling the ```context.Publish<OrderSubmittedEvent>()``` method. Copy the properties from the incoming `SubmitOrderCommand` message, to the properties of the event.

```c#
await context.Publish<OrderSubmittedEvent>(e =>
{
    e.OrderId = order.Id;
    e.CustomerId = message.CustomerId;
    e.Products = message.Products;
});
```

## Exercise 2.2: handle `OrderSubmittedEvent` in Shipping, Finance and Customers

In this exercise, you'll handle the `OrderSubmittedEvent` by logging the information in the `OrderSubmittedHandler` class in `Divergent.Shipping` project and in the `OrderSubmittedHandler` class in `Divergent.Sales` project. Then you'll extend the handler implementation in the `Divergent.Finance` project, in order to process the payment using the provided `GetAmount()` method and the `ReliablePaymentClient` class.

### Step 1

Create the `OrderSubmittedHandler` class in the `Divergent.Shipping` project, inside the `Handlers` namespace.

### Step 2

The `OrderSubmittedHandler` should process the `OrderSubmittedEvent` published by `Divergent.Sales`. In order to handle this event implement the `IHandleMessages<OrderSubmittedEvent>` interface in the `OrderSubmittedHandler` class.

### Step 3

In the `App.config` file in the `Divergent.Shipping` project, add a new configuration section called `UnicastBusConfig` and specify `MessageEndpointMappings` for the assembly containing `OrderSubmittedEvent` event. The route information provided in the mapping is used by NServiceBus internally to send subscription requests from the subscriber to the publisher.

```xml
<configSections>
  <section name="UnicastBusConfig" type="NServiceBus.Config.UnicastBusConfig, NServiceBus.Core" />
</configSections>
<UnicastBusConfig>
  <MessageEndpointMappings>
    <add Assembly="Divergent.Sales.Messages" Endpoint="Divergent.Sales" />
  </MessageEndpointMappings>
</UnicastBusConfig>
```

### Step 4

Use the provided logger to log information that the event was received and handled.

```c#
namespace Divergent.Shipping.Handlers
{
    public class OrderSubmittedHandler : IHandleMessages<OrderSubmittedEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger<OrderSubmittedHandler>();

        public async Task Handle(OrderSubmittedEvent message, IMessageHandlerContext context)
        {
            Log.Info("Handle");
        }
    }
}
```

### Step 5

Create an `OrderSubmittedHandler` class in the `Divergent.Finance` project, inside the `Handlers` namespace.

### Step 6

The `OrderSubmittedHandler` should also process the `OrderSubmittedEvent` published by `Divergent.Sales`. In order to handle this implement the `IHandleMessages<OrderSubmittedEvent>` interface in the `OrderSubmittedHandler` class.

### Step 7

In the `App.config` file in the `Divergent.Finance` project, add a new configuration section called `UnicastBusConfig` and specify `MessageEndpointMappings` for the assembly containing `OrderSubmittedEvent` event.

```xml
<configSections>
  <section name="UnicastBusConfig" type="NServiceBus.Config.UnicastBusConfig, NServiceBus.Core" />
</configSections>

<UnicastBusConfig>
  <MessageEndpointMappings>
    <add Assembly="Divergent.Sales.Messages" Endpoint="Divergent.Sales" />
  </MessageEndpointMappings>
</UnicastBusConfig>
```

### Step 8

When Finance receives the `OrderSubmittedEvent` message it needs to keep track of item prices that belong to the submitted order. And finally initiate the payment process by sending the `InitiatePaymentProcessCommand` message.

```c#
namespace Divergent.Finance.Handlers
{
    public class OrderSubmittedHandler : IHandleMessages<OrderSubmittedEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger<OrderSubmittedHandler>();

        public async Task Handle(OrderSubmittedEvent message, IMessageHandlerContext context)
        {
            Log.Info("Handle OrderSubmittedEvent");

            double amount = 0;
            using (var db = new FinanceContext())
            {
                var query = from price in db.Prices
                            where message.Products.Contains(price.ProductId)
                            select price;

                foreach (var price in query)
                {
                    var op = new OrderItemPrice()
                    {
                        OrderId = message.OrderId,
                        ItemPrice = price.ItemPrice,
                        ProductId = price.ProductId
                    };

                    amount += price.ItemPrice;

                    db.OrderItemPrices.Add(op);
                }

                await db.SaveChangesAsync();
            }

            await context.SendLocal(new InitiatePaymentProcessCommand()
            {
                CustomerId = message.CustomerId,
                OrderId = message.OrderId,
                Amount = amount
            });
        }
    }
}
```

### Step 9

In the `Divergent.Finance` project create the `InitiatePaymentProcessCommandHandler` class inside the `Handlers` namespace in order to handle the payment process.

```c#
namespace Divergent.Finance.Handlers
{
    public class InitiatePaymentProcessCommandHandler : IHandleMessages<InitiatePaymentProcessCommand>
    {
        private static readonly ILog Log = LogManager.GetLogger<InitiatePaymentProcessCommand>();
        private readonly ReliablePaymentClient _reliablePaymentClient;

        public InitiatePaymentProcessCommandHandler(ReliablePaymentClient reliablePaymentClient)
        {
            _reliablePaymentClient = reliablePaymentClient;
        }

        public async Task Handle(InitiatePaymentProcessCommand message, IMessageHandlerContext context)
        {
            Log.Info("Handle InitiatePaymentProcessCommand");

            await _reliablePaymentClient.ProcessPayment(message.CustomerId, message.Amount);
        }
    }
}
```

### Step 10

In the `Divergent.Customers` project create the `OrderSubmittedHandler` class inside the `Handlers` namespace in order to keep track of which orders have been submitted by which customer.

```c#
namespace Divergent.Customers.Handlers
{
    public class OrderSubmittedHandler : IHandleMessages<OrderSubmittedEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger<OrderSubmittedHandler>();
    
        public async Task Handle(OrderSubmittedEvent message, NServiceBus.IMessageHandlerContext context)
        {
            Log.Info("Handling: OrderSubmittedEvent.");
    
            using (var db = new CustomersContext())
            {
                var customer = await db.Customers
                    .Include(c=>c.Orders)
                    .SingleAsync(c=>c.Id == message.CustomerId);
    
                customer.Orders.Add(new Data.Models.Order()
                {
                    CustomerId = message.CustomerId,
                    OrderId = message.OrderId
                });
    
                await db.SaveChangesAsync();
            }
        }
    }
}
```

### Step 11

In the `App.config` file in the `Divergent.Customers` project add a new configuration section called `UnicastBusConfig` and specify `MessageEndpointMappings` for the assembly containing `OrderSubmittedEvent` event.

```xml
<configSections>
    <section name="UnicastBusConfig" type="NServiceBus.Config.UnicastBusConfig, NServiceBus.Core" />
</configSections>

<UnicastBusConfig>
  <MessageEndpointMappings>
    <add Assembly="Divergent.Sales.Messages" Endpoint="Divergent.Sales" />
  </MessageEndpointMappings>
</UnicastBusConfig>
```



## Exercise 2.3: create and publish the `PaymentSucceededEvent`

In this exercise we'll create a new event called `PaymentSucceededEvent`. This event will be published by the `InitiatePaymentProcessCommandHandler` in the `Divergent.Finance` project.

### Step 1

In the `Divergent.Finance.Messages` project, in the directory `Events`, add a new class called `PaymentSucceededEvent.cs`. The class should have only a single property with a public setter and a getter: id of the order.

```c#
namespace Divergent.Finance.Messages.Events
{
    public class PaymentSucceededEvent
    {
        public int OrderId { get; set; }
    }
}
```

### Step 2

At the end of `InitiatePaymentProcessCommandHandler`, publish the `PaymentSucceededEvent` by calling `context.Publish<PaymentSucceededEvent>()` method. Copy the order id from the incoming `InitiatePaymentProcessCommand` message, to the property of the event.

```c#
public async Task Handle(InitiatePaymentProcessCommand message, IMessageHandlerContext context)
{
    Log.Info("Handle InitiatePaymentProcessCommand");

    await _reliablePaymentClient.ProcessPayment(message.CustomerId, message.Amount);

    await context.Publish<PaymentSucceededEvent>(e =>
    {
        e.OrderId = message.OrderId;
    });
}
```

## Exercise 2.4: handle `PaymentSucceededEvent`

In this exercise we'll handle the `PaymentSucceededEvent` by logging the information in the `PaymentSucceededHandler` class in `Divergent.Shipping`.

### Step 1

Create the `PaymentSucceededHandler` class in the `Divergent.Shipping` project, in the `Handlers` namespace.

### Step 2

The `PaymentSucceededHandler` should process the `PaymentSucceededEvent` published by `Divergent.Finance`. In order to handle this event implement the `IHandleMessages<PaymentSucceededEvent>` interface in the `PaymentSucceededEvent` class.

### Step 3

Use the provided logger to log information that the event was received and handled.

```c#
namespace Divergent.Shipping.Handlers
{
    public class PaymentSucceededHandler : IHandleMessages<PaymentSucceededEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger<PaymentSucceededHandler>();

        public async Task Handle(PaymentSucceededEvent message, IMessageHandlerContext context)
        {
            Log.Info("Handle");
        }
    }
}
```

### Step 4

In the `App.config` file in the `Divergent.Shipping` project, add `MessageEndpointMappings` for the assembly containing `PaymentSucceededEvent`.

```xml
<UnicastBusConfig>
  <MessageEndpointMappings>
    <add Assembly="Divergent.Finance.Messages" Endpoint="Divergent.Finance" />
    <add Assembly="Divergent.Sales.Messages" Endpoint="Divergent.Sales" />
  </MessageEndpointMappings>
</UnicastBusConfig>
```



## Advanced exercise 2.5 : monitoring endpoints

You are not expected to finish this <u>advanced exercise</u>. It was added for those that finish the exercises well within time and to show the capabilities of the Particular Software platform. You also have the ability to finish this advanced exercise outside of the workshop. If you have questions, you can ask them during the workshop or using the Particular Software [free support channel in Google Groups](https://groups.google.com/forum/#!forum/particularsoftware).

Since this is an advanced exercise, you are likely required to read documentation to finish the exercise. Links to documentation will be provided.

### Step 1

In Visual Studio, open the project `Divergent.Customers` and have a look at the endpoint configuration. There should be configuration for forwarding messages to the audit queue and sending poison messages to the error queue.

You can read much more about auditing messages and different ways to configure this [in our documentation](https://docs.particular.net/nservicebus/operations/auditing).

### Step 2

Verify if the queues are created. You can use Windows' Computer Management tool. Press `ctrl` and `x` to open the menu in Windows and select 'Computer Management'. Then find MSMQ and verify if the queues are created under 'Private Queues'. 

Note: The MSMQ MMC snap-in is very limit. [QueueExplorer](http://www.cogin.com/mq/) is a great tool which provides more value.

### Step 3

If you've properly set up ServiceControl, it should already be running and have processed messages while running your exercises. Let's have a look at ServicePulse.

Open the browser at http://localhost:9090/

Note: If ServicePulse doesn't seem to be running, or it cannot connect to ServiceControl, you can either verify if the proper ServiceControl instance is started. Or you can check 'Services' in Windows itself to see if both services (ServicePulse and ServiceControl) are running. By default, all services should start with the name 'Particular' in front of it.

### Step 4

If you have successfully opened ServicePulse, you can see it is informing us via the list of 'Last 10 events' that it received messages from an endpoint, but it is not monitored yet. We need to set up the monitoring plugin first.

### Step 5

Let's install the Particular Software **heartbeat plugin**. You can find [documentation here](https://docs.particular.net/servicecontrol/plugins/heartbeat). Install this plugin into every project that hosts an endpoint, via the NuGet user interface or via the 'Package Manager Console'.

### Step 6

The **heartbeat plugin** works using a different queue than audit and error messages. You can read in the documentation how to configure NServiceBus and tell it which queue it should send to. 

You can find the name of the queue by accessing the 'ServiceControl Management' tool, which you can find in the Windows Start menu. The name of the instance is also the name of the queue.

Make sure you configure every project that hosts an endpoint. You can easily copy & paste this to every project, as the queue doesn't (and shouldn't) change in every project.

### Step 7

Run the solution and check ServicePulse while it starts.

After a while the 'Last 10 events' should show that `Divergent.Customers` or any other endpoint should have started. After a while it will show that these endpoints should be running the heartbeats plugin.

### Step 8

Turn off the endpoints by stopping debugging in Visual Studio or shutting down the console windows.

Remember that ServiceControl is expecting heartbeat messages to come in. If it won't receive those it will wait a little while longer before immediately reporting an endpoint as being down. But if you wait for half a minute or so, ServicePulse should report the endpoints being down.

After starting up the solution, ServicePulse should report the endpoints are working again.

### Step 9

At the top of the page in ServicePulse, you see a menu with various options. Check 'Failed Messages' if there are any messages that you weren't able to process in the past. Check how they can be group and retried individually or per group.

This is a powerful feature that can be used to retry message. But discuss this with, for example, the operations department. Imagine a system with a high throughput, but to performance maintenance, the database with your business data was brought offline for a couple of minutes. This could mean thousands of messages will end up in the error queue and thus in ServiceControl.

Once the system is up and running again, it has to handle the high throughput again. Should we retry all those messages we could introduce a spike of messages the system might be able to deal with. Resulting in more error messages, which could could introduce the same problem.

### Step 10

We now have a dashboard that can inform us when an endpoint is going down. A few things should be mentioned.

- Operations doesn't want to monitor a dashboard the entire day. Luckily ServiceControl also uses publish/subscribe to notify any subscribers using messages. You can build a special endpoint that subscribes to ServiceControl its integration events and decide how operations should be informed. By email, sms or anything else. Read more about [integration events](https://docs.particular.net/servicecontrol/contracts).
- You might notice several endpoints with the same name. Endpoints send heartbeats by providing a unique host identifier, made up of their endpoint name and a hash of the folder the endpoint is installed in.
  Our exercises all have the same endpoint name, but different folders. Another example is when you deploy endpoints using [Octopus](https://octopus.com/). This will deploy every version in its own folder, with the result that every version will spawn a new monitored endpoint in ServicePulse. You can solve this by [overriding the host identifier](https://docs.particular.net/nservicebus/hosting/override-hostid) yourself.