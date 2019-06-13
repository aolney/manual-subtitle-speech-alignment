module App

open Fable.Core
open Fable.Core.JsInterop
//open Fable.Import
//open Fable.Import.Browser
open Browser
open Browser.Types
//open Browser.Dom
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Elmish.Debug
open Elmish.HMR
open Thoth.Json
// open System.IO

open Fulma
open Fable.FontAwesome
open Fable.FontAwesome.Free

//open Elmish.Browser.Navigation
//open Elmish.Browser.UrlParser

// importAll "../sass/main.sass"

//Fable 2 transition
let inline toJson x = Encode.Auto.toString(0, x)
let inline ofJson<'T> json = Decode.Auto.unsafeFromString<'T>(json)

// TESTS
let randomFeature() = [1;2;3]


// Domain
// ---------------------------------------

/// Rather specific to https://github.com/aolney/SouthParkTTSData , but could be modified without loss of generality
type Datum = 
  {
    Start : int
    Stop: int
    Text: string
    ///Numerics expanded to words, e.g. $4 --> "four dollars"
    ExpandedText : string
    Id: string
    WavFile: string
    FrontAligned: bool
    EndAligned: bool
    SumAlignmentDiff: int
    ProportionAligned: float
  }

type Model = 
  {
    Index : int
    Data : Datum[]
  }

type Msg =
    | UpdateInput of string
    // | KeyUp of float
    | KeyDown of float

/// Keys unlikely to be used when manually correcting data; remap to functions
module Keys =
    let [<Literal>] Tab = 9.
    let [<Literal>] Enter = 13.
    let [<Literal>] Ctrl = 17.
    let [<Literal>] Alt = 18.
    let [<Literal>] Escape = 27.
    let [<Literal>] Left = 37.
    let [<Literal>] Up = 38.
    let [<Literal>] Right = 39.
    let [<Literal>] Down = 40.


let subscribeToKeyEvents dispatch =
    window.addEventListener("keydown", fun ev ->
        KeyDown (ev :?> KeyboardEvent).keyCode  |> dispatch )
    // window.addEventListener("keyup", fun ev ->
    //     KeyUp (ev :?> KeyboardEvent).keyCode  |> dispatch )

let init () : Model * Cmd<Msg> =
  ( { 
      Index = 0; 
      Data = [||] 
    }, [subscribeToKeyEvents] )

  
// Update
// ---------------------------------------
let update msg model =
  match msg with
  | UpdateInput(input) ->
      ({ model with Text = input}, [])
  // | KeyUp code ->
  //   match code with
  //   | Keys.Left -> model,[]
  //   | _ -> model,[]
  | KeyDown code ->
    match code with
    | Keys.Left -> model,[]
    | _ -> model,[]

// View
// ---------------------------------------
let simpleButton txt action dispatch =
  div 
    [ ClassName "column is-narrow" ]
    [ a
        [ ClassName "button"
          OnClick (fun _ -> action |> dispatch) ]
    [ str txt ] ]

let view model dispatch =
  div [ ClassName "columns is-vcentered" ] [ 
    div [ ClassName "column" ] [ 
      h1 [ ClassName "title"] [ str "Manual Subtitle Speech Alignment"]
      div [ ClassName "content"] [
        p [] [ str "Load and correct one wav file's worth of speech alignment data at a time. Click on the cat in the corner for more information." ]
        ul [] [
          str "instructions "
          li [] [ str "keymapping 1" ]
          li [] [ str "keymapping 2" ]
        ]
        textarea [
                    ClassName "input"
                    DefaultValue model.Data.[model.Index].Text
                    Size 200.0
                    Style [
                        Width "100%"
                        Height "100px"
                    ] 
                    OnInput (fun ev ->  UpdateInput (ev.target?value) |> dispatch )
                ] []
        //simpleButton "Go" ProcessInput dispatch
        // hr []
        // br []
        // Table.table [ Table.IsStriped ] [
        //     createHead( model )
        //     tbody [] [
        //         for result in model.Results do
        //             yield tr [] [
        //                 td [] [str result.Input]
        //                 td [] [str result.Output]
        //             ]
        //           ]
        //       ]
        //model.Results https://github.com/fable-compiler/static-web-generator/blob/master/src/Main.fs
      ]    
    ]
  ]  

// App
Program.mkProgram init update view
//|> Program.toNavigable (parseHash pageParser) urlUpdate
#if DEBUG
|> Program.withDebugger
//|> Program.withHMR
#endif
|> Program.withReactBatched "elmish-app" //withReactBatched //withReactSynchronous fails
|> Program.run
