namespace Shared

type Counter = { Value : int }

type Route = 
    | InitialCounter 
    | Increment 
    | Decrement
    
    | List
    | Create
    | Delete

module Route =
    let builder (route : Route) = 
        sprintf "/api/%s" (route.ToString())