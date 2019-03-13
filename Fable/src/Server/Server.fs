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

open Storage
open Microsoft.Azure.Management.Fluent
open Microsoft.WindowsAzure.Storage
open Microsoft.Azure.Management.ResourceManager
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent

//https://stackoverflow.com/questions/52630058/can-i-list-azure-resource-groups-from-a-local-c-sharp-application

let serializer = ThothSerializer()

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = tryGetEnv "public_path" |> Option.defaultValue "../Client/public" |> Path.GetFullPath
let storageAccount = tryGetEnv "STORAGE_CONNECTIONSTRING" |> Option.defaultValue "UseDevelopmentStorage=true" |> CloudStorageAccount.Parse
let port = "SERVER_PORT" |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let getInitCounter () : Counter = { Value = 42 }

let mutable counter = getInitCounter()

let incrementHandler =
     counter <- { counter with Value = counter.Value + 1}
     Encode.Auto.toString(0,counter) |> text

let decrementHandler =
     counter <- { counter with Value = counter.Value - 1}
     Encode.Auto.toString(0,counter) |> text

let listHandler =
     let sas = azure.StorageAccounts.List().ToArray()
     let saNames = sas |> Array.map (fun sa -> sa.Name)
     json saNames

let createHandler =
     text "not implemented"

let deleteHandler =
     text "not implemented"

let webApp =
    choose [
           routeCi (Route.builder InitialCounter) >=> (Encode.Auto.toString(0,counter) |> text)
           routeCi (Route.builder Increment) >=> incrementHandler
           routeCi (Route.builder Decrement) >=> decrementHandler

           //for next week
           routeCi (Route.builder List) >=> warbler (fun _ -> listHandler)
           routeCi (Route.builder Create) >=> createHandler
           routeCi (Route.builder Delete) >=> deleteHandler

           //later          
           //routeCi (Route.builder CreateSASToken) >=> x
           //routeCi (Route.builder ChangeKey) >=> x
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