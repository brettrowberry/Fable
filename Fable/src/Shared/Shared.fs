namespace Shared

type Route =    
    | List
    | Create of string
    | Delete
    | CreateSASToken of string

/// won't serialize nicely, but cool
// type SASTokenState =
//     | Loading
//     | Unloaded
//     | Loaded of string

type FableStorageAccount = 
    { 
        Id : string
        Name : string
        Region : string
        Tags : (string * string) []
        SASToken : string option
        SASLoading : bool option
    }

module Route =
    let builder (route : Route) = 
        match route with
        | List | Delete -> sprintf "/api/%s" (route.ToString())
        | Create nickname -> sprintf "/api/Create/%s" nickname
        | CreateSASToken id -> sprintf "/api/CreateSASToken/%s" id