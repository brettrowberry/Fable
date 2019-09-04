module Storage

open System
open System.Collections.Generic
open System.Linq
open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.Storage.Fluent
open Microsoft.Azure.Management.Storage.Fluent.Models
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Core

let azureStorage =
    let azure =
        let subscription = "06279c1b-6b5a-4089-8b9a-754f69a378fb"
        let creds = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                        clientId = "523fb00f-ee33-495b-b433-6e2240abf5c4",
                        clientSecret = "g:y/Vwu2QaWjYZ38*7RK_biSSw8QR.py",
                        tenantId = "b41bb662-23d3-4774-bf22-934d7cf1b337",
                        environment = AzureEnvironment.AzureGlobalCloud)
        Azure.Configure().Authenticate(creds).WithSubscription(subscription)
    azure.StorageAccounts

let getStorageAccounts () = azureStorage.List().ToArray()
let deleteStorageAccounts ids = azureStorage.DeleteByIdsAsync ids
let createSasToken id = "?sv=2018-03-28&ss=b&srt=sco&sp=rwdlac&se=2019-05-24T10:12:05Z&st=2019-05-24T02:12:05Z&spr=https&sig=Zj%2FRbnTzYYtJrtLmA7sipHrEpHtC%2By%2BXSq%2F20aLJbGI%3D"

let createStorageAccount nickname =
    let storageAccountName = Guid.NewGuid().ToString("N").Substring(0,24)
    let tags = Dictionary<string, string>(dict [ ("nickname", nickname) ])
    let sa = azureStorage.Define(storageAccountName)
               .WithRegion(Region.EuropeWest)
               .WithNewResourceGroup("sam-westeurope")
               .WithGeneralPurposeAccountKindV2()
               .WithSku(StorageAccountSkuType.Standard_LRS)
               .WithOnlyHttpsTraffic()
               .WithBlobStorageAccountKind()
               .WithAccessTier(AccessTier.Cool)
               .WithTags(tags)
               .Create()
    sa.Name