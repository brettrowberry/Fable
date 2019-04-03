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

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| ListAccounts of Result<FableStorageAccount [], exn>
| Create of string
| Delete of string

let fetchAccounts () =
  fetchAs ("http://localhost:8080" + Route.builder Route.List) (Decode.Auto.generateDecoder<FableStorageAccount []>()) []

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Accounts = [||] }
    let loadCountCmd =
        Cmd.ofPromise
            fetchAccounts
            ()
            (Ok >> ListAccounts)
            (Error >> ListAccounts)
    initialModel, loadCountCmd

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
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
  let header = tr [] [ th [] [ str "Name" ]; th [] [ str "Region" ] ]
  let tableRow name id = tr [] [ td [] [ str name ]; td [] [ str id ] ]
  let data = [ for x in model -> tableRow x.name x.region ]
  let tableContents = header :: data
  table [] tableContents

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