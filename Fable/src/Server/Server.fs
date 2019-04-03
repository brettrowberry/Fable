open System
open System.IO
open System.Linq
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http

open FSharp.Control.Tasks.V2
open Giraffe
open Shared
open Thoth.Json.Giraffe
open Thoth.Json.Net

open Microsoft.WindowsAzure.Storage
open Storage

//https://stackoverflow.com/questions/52630058/can-i-list-azure-resource-groups-from-a-local-c-sharp-application

let serializer = ThothSerializer()
let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let publicPath = tryGetEnv "public_path" |> Option.defaultValue "../Client/public" |> Path.GetFullPath
let storageAccount = tryGetEnv "STORAGE_CONNECTIONSTRING" |> Option.defaultValue "UseDevelopmentStorage=true" |> CloudStorageAccount.Parse
let port = "SERVER_PORT" |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

// let detailsHandler name = //TODO

let listHandler () =
     let sas = azureStorage.List().ToArray()
     let saNames = sas |> Array.map (fun sa -> { name = sa.Name; region = sa.RegionName } )
     json saNames

let createHandler nickname = 
     let name = createStorageAccount nickname
     sprintf "created '%s' with nickname '%s'" name nickname |> text

let deleteHandler name =
     deleteStorageAccount name
     sprintf "deleted '%s'" name |> text

let webApp =
    choose [
           routeCi (Route.builder Route.List) >=> warbler (fun _ -> listHandler ())
           routeCif "/api/Create/%s" createHandler
           routeCif "/api/Delete/%s" deleteHandler

           //later
           //get storage account details        
           //routeCi (Route.builder CreateSASToken) >=> x
           //routeCi (Route.builder ChangeKey) >=> x
           
           setStatusCode 404 >=> text "Not Found"
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe()
    |> ignore
    tryGetEnv "APPINSIGHTS_INSTRUMENTATIONKEY" |> Option.iter (services.AddApplicationInsightsTelemetry >> ignore)

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()