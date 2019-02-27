open System
open System.IO
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

let webApp =
    choose [
           routeCi (Route.builder InitialCounter) >=> (Encode.Auto.toString(0,counter) |> text)
           routeCi (Route.builder Increment) >=> incrementHandler
           routeCi (Route.builder Decrement) >=> decrementHandler
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore
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