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

type Model = { Accounts : FableStorageAccount [] }

type Msg =
| ListAccounts of Result<FableStorageAccount [], exn>
| Create of string
| Delete of string

let fetchAccounts () =
  fetchAs ("http://localhost:8080" + Route.builder Route.List) (Decode.Auto.generateDecoder<FableStorageAccount []>()) []

let init () : Model * Cmd<Msg> =
    let initialModel = { Accounts = [||] }
    let loadCountCmd =
        Cmd.ofPromise
            fetchAccounts
            ()
            (Ok >> ListAccounts)
            (Error >> ListAccounts)
    initialModel, loadCountCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ListAccounts (Ok accounts)->
        let nextModel = { Accounts = accounts }
        nextModel, Cmd.none
    | ListAccounts (Error err)->
        Fable.Import.Browser.console.debug err.Message
        currentModel, Cmd.none
    | _ -> currentModel, Cmd.none

//https://fulma.github.io/Fulma/#fulma/layouts/columns
//https://fulma.github.io/Fulma/#fulma/elements/table
let viewAccounts (model : FableStorageAccount [] ) (dispatch : Msg -> unit) =
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
        [ viewAccounts model.Accounts dispatch ]

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