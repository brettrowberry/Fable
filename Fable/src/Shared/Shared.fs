namespace Shared

type Route =    
    | List
    | Create
    | Delete

module Route =
    let builder (route : Route) = 
        sprintf "/api/%s" (route.ToString())