namespace Shared

type Route =    
    | List
    | Create of string
    | Delete

type FableStorageAccount = 
    { 
        Id : string
        Name : string
        Region : string
        Tags : (string * string) []
    }

module Route =
    let builder (route : Route) = 
        match route with
        | List | Delete -> sprintf "/api/%s" (route.ToString())
        | Create nickname -> sprintf "/api/Create/%s" nickname