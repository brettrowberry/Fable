module Storage

open System
open System.Collections.Generic
open System.Linq
open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.Storage.Fluent
open Microsoft.Azure.Management.Storage.Fluent.Models
open Microsoft.Azure.Management.ResourceManager.Fluent

let azureStorage = 
    let azure =
        let subscription = "06279c1b-6b5a-4089-8b9a-754f69a378fb"
        let creds = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                        clientId = "8bc681b1-9de0-4147-9a30-18d0fcedf398", 
                        clientSecret = "jtkDJ5s09kdYaAsit1DB0zg1gE3ECh2kd0QI5yyC95o=", 
                        tenantId = "b41bb662-23d3-4774-bf22-934d7cf1b337", 
                        environment = AzureEnvironment.AzureGlobalCloud)
        Azure.Configure().Authenticate(creds).WithSubscription(subscription)
    azure.StorageAccounts

let resourceGroup = "fable-rg"

let getStorageAccounts () = azureStorage.List().ToArray()
let getStorageAccount name = azureStorage.GetByResourceGroup(resourceGroup, name)
let deleteStorageAccounts ids = azureStorage.DeleteByIdsAsync ids
//TODO get SAS token
//TODO change key

let createStorageAccount nickname =
    let storageAccountName = Guid.NewGuid().ToString("N").Substring(0,24)
    let tags = Dictionary<string, string>(dict [ ("nickname", nickname) ])
    let sa = azureStorage.Define(storageAccountName)
               .WithRegion("southcentralus")
               .WithNewResourceGroup("fable-rg")
               .WithGeneralPurposeAccountKindV2()
               .WithSku(StorageAccountSkuType.Standard_LRS)
               .WithOnlyHttpsTraffic()
               .WithBlobStorageAccountKind()
               .WithAccessTier(AccessTier.Cool)
               .WithTags(tags)
               .Create()
    sa.Name