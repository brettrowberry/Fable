module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared

open Fulma
open Fulma.Extensions.Wikiki

type CreateModel = {
   IsProcessing : bool
   Nickname: string option
}

type Model = { 
   Accounts : FableStorageAccount []
   Error : exn option
   IsProcessing : bool
   SelectedIds : Set<string>
   CanDelete: bool
   CreateView: CreateModel option
}

type CreateMsg =
  | Create 
  | CreateOk of string
  | NicknameBox of string

type Msg =
| ListAccounts of FableStorageAccount []
| CreateMsg of CreateMsg
| ErrorMsg of exn
| New 
| RemoveError 
| Delete
| DeleteOk of string
| ToggleSelect of string
| CreateSASToken of string
| CreatedSASToken of id: string * token: string

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

let createSASTokenCmd (id : string) =
    Cmd.ofPromise
        (fun _ -> promise {
            let! resp = fetch ("http://localhost:8080" + Route.builder (Route.CreateSASToken id)) []
            let! text = resp.text()
            return id, text
        })
        ()
        CreatedSASToken
        ErrorMsg
        
let init () : Model * Cmd<Msg> =
    let initialModel = { 
      Accounts = [||]
      Error = None
      IsProcessing = false
      SelectedIds = Set.empty
      CanDelete = false
      CreateView = None }
    initialModel, listAccountsCmd

let updateCreate (msg : CreateMsg) (model : CreateModel) =
  match msg with
  | Create _ ->
    let createCmd : Cmd<Msg> =
            Cmd.ofPromise 
                createAccount
                model.Nickname.Value
                (CreateOk >> CreateMsg)
                ErrorMsg  
    {IsProcessing = true; Nickname = None}, createCmd
  | CreateOk _ ->
    {model with IsProcessing = false}, listAccountsCmd
  | NicknameBox str ->
    {model with Nickname = Some(str)}, Cmd.none

let update (msg : Msg) (current : Model) : Model * Cmd<Msg> =
    match msg with
    | ListAccounts accounts ->
        {current with Accounts = accounts}, Cmd.none
    | ErrorMsg err ->
        Fable.Import.Browser.console.debug err.Message
        {current with Error = Some err; IsProcessing = false}, Cmd.none
    | RemoveError ->
        {current with Error = None}, Cmd.none    
    | CreateMsg cmsg -> 
      let cview,cmd = updateCreate cmsg current.CreateView.Value //unsafe
      {current with CreateView = Some cview }, cmd
    | Delete ->
        {current with IsProcessing = true; CanDelete = false}, deleteAccountsCmd (Array.ofSeq current.SelectedIds )
    | DeleteOk _ -> 
        {current with IsProcessing = false; SelectedIds = Set.empty; CanDelete = true}, listAccountsCmd
    | ToggleSelect id ->
        let newSet = 
          if current.SelectedIds.Contains id
          then current.SelectedIds.Remove id
          else current.SelectedIds.Add id
        {current with SelectedIds = newSet; CanDelete = newSet.Count > 0 }, Cmd.none   
    | CreateSASToken id ->
      let accounts =
        current.Accounts
        |> Array.map (fun x -> if x.Id = id then {x with SASLoading = Some true} else x)
      {current with Accounts = accounts}, createSASTokenCmd id
    | CreatedSASToken(id,token) ->
      let accounts = 
        current.Accounts
        |> Array.map (fun x -> if x.Id = id then {x with SASToken = Some token} else x)
      {current with Accounts = accounts}, Cmd.none 
    | New ->
      {current with CreateView = Some({IsProcessing = false; Nickname = None})}, Cmd.none
    | _ ->
      Fable.Import.Browser.console.debug "Match Any"
      current, Cmd.none

let viewSpinner = div [ ClassName "lds-dual-ring" ] []

let viewCreate model dispatch =
  Container.container [] [
      Label.label [] [ str "Storage Account Nickname" ]
      Input.text [Input.OnChange(fun f -> NicknameBox f.Value |> dispatch); Input.Value (defaultArg model.Nickname "")]
      Button.a [Button.OnClick(fun _ -> Create |> dispatch); Button.Disabled (model.Nickname.IsNone)] 
        (seq {
          yield str "Create"
          if model.IsProcessing then yield viewSpinner
        })]

let viewCommands (dispatch : Msg -> unit) model =
  Container.container [] [
    Divider.divider [ ]
    Button.a [Button.OnClick(fun _ -> Delete |> dispatch); Button.Disabled (not model.CanDelete) ] [ str "Delete"]
    (match model.CreateView with
      | Some elem -> viewCreate elem (CreateMsg >> dispatch)
      | None -> Button.a [Button.OnClick(fun _ -> New |> dispatch) ] [ str "New"] ) 
  ]

let viewCreateSasTokenButton (sa : FableStorageAccount) (dispatch : Msg -> unit) =
  match sa.SASToken with
  | Some(token) -> str token
  | None -> 
    Button.a [Button.OnClick(fun _ -> CreateSASToken sa.Id |> dispatch)] 
      (seq {
          yield str "New"
          if sa.SASLoading.IsSome && sa.SASLoading.Value then yield viewSpinner
        })

let viewError (model : exn) (dispatch : Msg -> unit) =
  Message.message [Message.Color IsDanger; Message.Size IsSmall] [ 
    Message.header [ ]
      [ str "Error"
        Delete.delete [ Delete.OnClick (fun _ -> dispatch RemoveError) ] [ ] ]
    Message.body [ ] [ str model.Message ] ]

let viewAccountRow isSelected (sa : FableStorageAccount) dispatch = 
  tr [] [ 
    td [] [ Checkradio.checkbox [ 
      Checkradio.Id sa.Name
      Checkradio.Checked isSelected
      Checkradio.OnChange (fun _ -> ToggleSelect sa.Id |> dispatch) ] []]
    td [] [ str (String.concat ", " (sa.Tags |> Array.map snd)) ]
    td [] [ str sa.Name ]
    td [] [ str sa.Region ]
    td [] [ viewCreateSasTokenButton sa dispatch]
    ]

let viewAccounts (model : Model ) (dispatch : Msg -> unit) =  
  let tableBody = 
    [ for x in model.Accounts -> 
      viewAccountRow (model.SelectedIds.Contains x.Id) x dispatch]
  Table.table [ Table.IsBordered; Table.IsStriped; Table.IsHoverable; Table.IsFullWidth ] [ 
    thead [] [
      tr [] [ 
        th [] [ str "Selected" ]
        th [] [ str "Nickname" ]
        th [] [ str "Name" ]
        th [] [ str "Region" ]
        th [] [ str "SAS Token" ]
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
          yield viewAccounts model dispatch      
          yield viewCommands dispatch model
           })

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