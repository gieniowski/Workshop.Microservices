## ASP.Net Core demos

These samples demo UI Composition techniques on top of .Net Core (v1.1.2). The demo is composed by 2 samples.

### Divergent.CompositionGateway - sample

`Divergent.CompositionGateway` shows how to create and host a .Net Core API Gateway, or reverse proxy, that composes http requests to multiple API back-ends. To run this sample ensure that the following projects are set as startup projects:

* `Divergent.Sales.API.Host`
* `Divergent.Shipping.API.Host`
* `Divergent.CompositionGateway`

As client to test the functionality a REST client such as [Postman](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop?hl=en) can be used.

### Divergent.Website - sample

`Divergent.Website` sample is a .Net Core Mvc app that composes http requests to multiple back-ends directly in the Mvc Views.  To run this sample ensure that the following projects are set as startup projects:

* `Divergent.Sales.API.Host`
* `Divergent.Shipping.API.Host`
* `Divergent.Website`