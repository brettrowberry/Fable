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

open Microsoft.WindowsAzure.Storage
open Storage

//https://stackoverflow.com/questions/52630058/can-i-list-azure-resource-groups-from-a-local-c-sharp-application

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let publicPath = tryGetEnv "public_path" |> Option.defaultValue "../Client/public" |> Path.GetFullPath
let storageAccount = tryGetEnv "STORAGE_CONNECTIONSTRING" |> Option.defaultValue "UseDevelopmentStorage=true" |> CloudStorageAccount.Parse
let port = "SERVER_PORT" |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

// let detailsHandler name = //TODO

let listHandler () =
     let sas = azureStorage.List().OrderBy(fun s -> s.CreationTime).ToArray()
     let saNames =
          sas 
          |> Array.map (fun sa -> { 
               Id = sa.Id; 
               Name = sa.Name; 
               Region = sa.RegionName;
               SASToken = None;
               SASLoading = None;
               Tags = sa.Tags 
                    |> Seq.map (fun t -> (t.Key, t.Value)) 
                    |> Seq.toArray })
     json saNames

let createHandler nickname = 
     let name = createStorageAccount nickname
     sprintf "created '%s' with nickname '%s'" name nickname |> text

let createSASTokenHandler id = 
     let sasToken = createSasToken id
     System.Threading.Thread.Sleep 3000
     sasToken |> text

let deletesHandler : HttpHandler =
     fun (next : HttpFunc) (ctx : HttpContext) ->
          task {
               let! ids = ctx.BindJsonAsync<string []>()
               let! results = deleteStorageAccounts ids
               let returnText = String.concat "," results |> text
               return! returnText next ctx
          }

let webApp =
     choose [
             GET  >=> choose [
                 routeCi (Route.builder Route.List) >=> warbler (fun _ -> listHandler ())
                 routeCif "/api/Create/%s" createHandler
               //   routeCi (Route.builder Route.CreateSASToken) >=> createSASTokenHandler
                 routeCif "/api/CreateSASToken/%s" createSASTokenHandler
                 //routeCi (Route.builder ChangeKey) >=> x
             ]
             POST >=> choose [
                 routeCi (Route.builder Route.Delete) >=> deletesHandler
             ]
             RequestErrors.NOT_FOUND "Not Found"
         ]


let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore
    services.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Thoth.Json.Giraffe.ThothSerializer()) |> ignore
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