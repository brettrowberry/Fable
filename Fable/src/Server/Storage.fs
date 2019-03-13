module Storage

open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication
open System

type AuthenticationCredentials = { ClientId : Guid; ClientSecret : string; TenantId : Guid }

type [<NoComparison>] AuthenticatedContext = AuthenticatedContext of IResourceManager

/// Authenticates to Azure using the supplied credentials for a specific subscription.
let authenticate (credentials:AuthenticationCredentials) (subscriptionId:Guid) =
    let spi = AzureCredentialsFactory().FromServicePrincipal(string credentials.ClientId, credentials.ClientSecret, string credentials.TenantId, AzureEnvironment.AzureGlobalCloud)
    ResourceManager
        .Authenticate(spi)
        .WithSubscription(string subscriptionId)
        |> AuthenticatedContext

let c : AuthenticationCredentials = { 
    ClientId = Guid.Parse "8bc681b1-9de0-4147-9a30-18d0fcedf398"
    ClientSecret = "jtkDJ5s09kdYaAsit1DB0zg1gE3ECh2kd0QI5yyC95o="
    TenantId = Guid.Parse "b41bb662-23d3-4774-bf22-934d7cf1b337"
    }

let s = "06279c1b-6b5a-4089-8b9a-754f69a378fb"

let generateStorageAccountName() =
    Guid.NewGuid().ToString("N").Substring(0,24)