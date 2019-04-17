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
open Fulma.Extensions.Wikiki

type Model = { Accounts : FableStorageAccount []
               Error : exn option
               IsProcessing : bool
               SelectedIds : Set<string>}

type Msg =
| ListAccounts of FableStorageAccount []
| Create of string
| CreateOk of string
| ErrorMsg of exn
| RemoveError 
| Delete
| DeleteOk of string
| ToggleSelect of string

let fetchAccounts () =
  fetchAs ("http://localhost:8080" + Route.builder Route.List) (Decode.Auto.generateDecoder<FableStorageAccount []>()) []

let createAccount (name : string) =
  promise {
    let! resp = fetch ("http://localhost:8080" + Route.builder (Route.Create name)) []
    let! text = resp.text()
    return text
  }
  
let deleteAccounts (ids : string[]) =
  promise {
    let! resp = postRecord<string []> ("http://localhost:8080" + Route.builder Route.Delete) ids []
    return! resp.text()
  }

let deleteAccountsCmd (ids : string[]) =
    Cmd.ofPromise
        deleteAccounts         
        ids
        DeleteOk
        ErrorMsg

let listAccountsCmd =
    Cmd.ofPromise
        fetchAccounts
        ()
        ListAccounts
        ErrorMsg

let init () : Model * Cmd<Msg> =
    let initialModel = { Accounts = [||]; Error = None; IsProcessing = false; SelectedIds = Set.empty}
    initialModel, listAccountsCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ListAccounts accounts ->
        {currentModel with Accounts = accounts}, Cmd.none
    | ErrorMsg err ->
        Fable.Import.Browser.console.debug err.Message
        {currentModel with Error = Some err; IsProcessing = false}, Cmd.none
    | RemoveError ->
        {currentModel with Error = None}, Cmd.none    
    | Create nickname -> //throw up a spinner on the create button?
        let createCmd =
            Cmd.ofPromise 
                createAccount
                nickname
                CreateOk
                ErrorMsg        
        {currentModel with IsProcessing = true}, createCmd
    | CreateOk _ ->
        {currentModel with IsProcessing = false}, listAccountsCmd
    | Delete -> //todo diable delete button
        {currentModel with IsProcessing = true}, deleteAccountsCmd (Array.ofSeq currentModel.SelectedIds )
    | DeleteOk _ -> 
        {currentModel with IsProcessing = false; SelectedIds = Set.empty}, listAccountsCmd
    | ToggleSelect id ->
        let newSet = 
          if currentModel.SelectedIds.Contains id
          then currentModel.SelectedIds.Remove id 
          else currentModel.SelectedIds.Add id
        {currentModel with SelectedIds = newSet}, listAccountsCmd               
    | _ -> 
      Fable.Import.Browser.console.debug "Match Any"
      currentModel, Cmd.none

//TODO make name an input
let viewCommands (dispatch : Msg -> unit) =
  Container.container [] [
    Button.a [Button.OnClick(fun _ -> Create "name" |> dispatch)] [ str "Create"]
  ]

let viewSpinner = div [ ClassName "lds-dual-ring" ] []

let viewError (model : exn) (dispatch : Msg -> unit) =
  //https://fulma.github.io/Fulma/#fulma/components/message
  Message.message [Message.Color IsDanger; Message.Size IsSmall] [ 
    Message.header [ ]
      [ str "Error"
        Delete.delete [ Delete.OnClick (fun _ -> dispatch RemoveError) ] [ ] ]
    Message.body [ ] [ str model.Message ] ]

let viewAccountRow isSelected id region dispatch = 
  tr [] [ 
    td [] [ Checkradio.checkbox [ 
      Checkradio.Id id
      Checkradio.Checked isSelected
      Checkradio.OnChange (fun _ -> ToggleSelect id |> dispatch) ] []]
    td [] [ str id ] 
    td [] [ str region ] ]

let viewAccounts (model : Model ) (dispatch : Msg -> unit) =
    //https://fulma.github.io/Fulma/#fulma/layouts/columns
    //https://fulma.github.io/Fulma/#fulma/elements/table
  
  let tableBody = 
    [ for x in model.Accounts -> 
      viewAccountRow (model.SelectedIds.Contains x.Name) x.Name x.Region dispatch]
  Table.table [ Table.IsBordered; Table.IsStriped; Table.IsHoverable; Table.IsFullWidth ] [ 
    thead [] [
      tr [] [ 
        th [] [ str "Selected" ]
        th [] [ str "Name" ]
        th [] [ str "Region" ] 
      ]
    ]
    tbody [] 
      tableBody
  ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        (seq {
          if model.Error.IsSome then 
            yield viewError model.Error.Value dispatch
          if model.IsProcessing then 
            yield viewSpinner          
          yield viewCommands dispatch
          yield viewAccounts model dispatch })

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