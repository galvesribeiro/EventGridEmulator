# EventGridEmulator
Microsoft Azure EventGrid Emulator

## How to use

`dotnet run` on the `EventGridEmulator` project directory

With the project running, POST the subscription object just like in the real Azure EventGrid:

```
POST http://localhost:5000/Microsoft.EventGrid/eventSubscriptions/{subscriptionName}

{
  "properties": {
    "destination": {
      "endpointType": "WebHook",
      "properties": {
        "endpointUrl": "http://localhost:5000/test"
      }
    },
    "filter": {
      "includedEventTypes": ["ALL"],
      "isSubjectCaseSensitive": true,
      "subjectBeginsWith": "Test",
      "subjectEndsWith": ".A"
    }
  }
}
```

After the subscription have been created, you can post Events to the `events` endpoint:

```
POST http://localhost:5000/Microsoft.EventGrid/events

[{
  "eventType": "recordInserted",
  "subject": "Test.A",
  "data": {
    "make": "Ducati",
    "model": "Monster"
  }
}]
```

The message will then be forwarded to the registered subscriptions.

> This is a work in progress. Docker/Kubernetes support along with other kind of message publishers will come soon.
