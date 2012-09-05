play.billing
============

A C# port of Google's Play Billing SDK sample. This port uses Mono for Anroid.  

The most important part of this port is that IMarketBillingService conversion was made with the following steps: 

1. The IMarketBillingService.aidl must be converted to a .java file. An IDE such as Eclipse can do this for you.  

2. Convert the .java file to C#.  

3. BillingServiceStub.AsInterface(...) can now be used instead of trying to directly cast IBinder to IMarketBillingService.  

This port is a close port, so no further refactoring was applied to allow the similarities to be seen. Refactor as needed on your local copy.  

One other thing to note, is that the BillingService inherits directly from Service, thus all logic will execute on the UI thread. Inherit from IntentService for to execute logic on a background worker thread.  

In order to initiate requests, use an instance of BillingService directly, such as:  
          
        billingService.RequestPurchase("net.myapp.item1");
        