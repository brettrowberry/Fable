module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared
open System.Globalization

open Fulma

type Model = { Accounts : FableStorageAccount []
               Error : exn option}

type Msg =
| ListAccounts of Result<FableStorageAccount [], exn>

| Create of string
| CreateOk of string //consider collapsing into CreateStastus
| CreateErr of exn

| Delete of string
| DeleteOk of Result<string,exn> //consider collapsing into DeleteStastus
| DeleteErr of Result<string,exn>

let fetchAccounts () =
  fetchAs ("http://localhost:8080" + Route.builder Route.List) (Decode.Auto.generateDecoder<FableStorageAccount []>()) []

let createAccount (name : string) =
  promise {
    let! resp = fetch ("http://localhost:8080" + Route.builder (Route.Create name)) []
    let! text = resp.text()
    return text
  }
  
let loadCountCmd =
    Cmd.ofPromise
        fetchAccounts
        ()
        (Ok >> ListAccounts)
        (Error >> ListAccounts)

let init () : Model * Cmd<Msg> =
    let initialModel = { Accounts = [||]; Error = None }
    initialModel, loadCountCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ListAccounts (Ok accounts)->
        let nextModel = { Accounts = accounts; Error = None }
        nextModel, Cmd.none
    | ListAccounts (Error err)->
        Fable.Import.Browser.console.debug err.Message
        currentModel, Cmd.none
    | Create nickname -> //throw up a spinner on the create button?
        let createCmd =
            Cmd.ofPromise 
                createAccount
                nickname
                CreateOk
                CreateErr         
        currentModel, createCmd
    | CreateOk _ ->
        currentModel, loadCountCmd          
    | _ -> 
      Fable.Import.Browser.console.debug "Match Any"
      currentModel, Cmd.none

let viewCommands (dispatch : Msg -> unit) =
  Container.container [] [
    Button.a [Button.OnClick(fun _ -> Create "name" |> dispatch)] [ str "Create"]
  ]  

let viewError (model : exn option) =
  //https://fulma.github.io/Fulma/#fulma/components/message
  Message.message [Message.Color IsDanger] [
    Delete.delete [ ] []
  ]

let viewAccounts (model : FableStorageAccount [] ) (dispatch : Msg -> unit) =
    //https://fulma.github.io/Fulma/#fulma/layouts/columns
    //https://fulma.github.io/Fulma/#fulma/elements/table
  let tableRow name id = tr [] [ td [] [ str name ]; td [] [ str id ] ]
  let tableBody = [ for x in model -> tableRow x.name x.region ]
  Table.table [ Table.IsBordered; Table.IsStriped; Table.IsHoverable; Table.IsFullWidth ] [ 
    thead [] [
      tr [] [ 
        th [] [ str "Name" ]
        th [] [ str "Region" ] 
      ]      
    ]
    tbody [] 
      tableBody
  ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ viewError model.Error
          viewCommands dispatch
          viewAccounts model.Accounts dispatch ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run