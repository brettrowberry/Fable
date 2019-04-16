namespace Shared

type Route =    
    | List
    | Create of string
    | Delete

type FableStorageAccount = 
    { 
        Name : string
        Region : string
        // tags : IReadOnlyDictionary<string,string> 
    }

module Route =
    let builder (route : Route) = 
        match route with
        | List | Delete -> sprintf "/api/%s" (route.ToString())
        | Create nickname -> sprintf "/api/Create/%s" nickname