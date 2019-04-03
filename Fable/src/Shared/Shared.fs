namespace Shared

type Route =    
    | List
    | Create
    | Delete

type FableStorageAccount = 
    { 
        name : string
        region : string
        // tags : IReadOnlyDictionary<string,string> 
    }

module Route =
    let builder (route : Route) = 
        sprintf "/api/%s" (route.ToString())