namespace Shared

type Counter = { Value : int }

type Route = InitialCounter | Increment | Decrement

module Route =
    let builder (route : Route) = 
        sprintf "/api/%s" (route.ToString())