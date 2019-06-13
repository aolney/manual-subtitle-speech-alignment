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

open WaveSurfer
open System.IO

//open Elmish.Browser.Navigation
//open Elmish.Browser.UrlParser

// importAll "../sass/main.sass"

//Fable 2 transition
let inline toJson x = Encode.Auto.toString(0, x)
let inline ofJson<'T> json = Decode.Auto.unsafeFromString<'T>(json)

// TESTS
let randomFeature() = [1;2;3]

//Globals
let [<Global>] URL: obj = jsNative
let [<Global>] Audio: obj = jsNative

let wavesurfer = 
  WaveSurfer.create(
     createObj [
      "container" ==> "#waveform"
      "waveColor" ==> "violet"
      "progressColor" ==> "purple"
  ]
)
// let audio = createNew Audio ()
// audio?src <- URL?createObjectURL("/y/south-park-1-to-20/1-1.wav")
// wavesurfer.load( audio )


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
    WavPath : string
    JsonPath : string
    Index : int
    Data : Datum[]
  }

type Msg =
    | UpdateText of string
    | UpdateExpandedText of string
    | UpdateWavPath of Browser.Types.FileList
    | UpdateJsonPath of Browser.Types.FileList
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
      WavPath = "*.wav"
      JsonPath = "*.json"
      Index = 0; 
      Data = [|   
        {
          Start= 184170
          Stop= 184284
          Text= "YES, HE CAN!"
          Id= "Cartman-1-1-184170-184284"
          WavFile= "1-1.wav"
          FrontAligned= true
          EndAligned= false
          SumAlignmentDiff= 185171
          ProportionAligned= 0.66666666666666663
          ExpandedText= "YES, HE CAN!"
        }
      |] 
    }, [subscribeToKeyEvents] )

  
// Update
// ---------------------------------------
let update msg model =
  match msg with
  | UpdateText(input) ->
      model.Data.[model.Index] <- { model.Data.[model.Index] with Text = input}
      ( model, [])
  | UpdateExpandedText(input) ->
      model.Data.[model.Index] <- { model.Data.[model.Index] with ExpandedText = input}
      ( model, [])
  | UpdateWavPath(input) ->
      ( {model with WavPath = input.[0].name}, [])
  | UpdateJsonPath(input) ->
      ( {model with JsonPath = input.[0].name}, [])
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
      //Heading.h1 [ ] [ str "Manual Subtitle Speech Alignment"]
      div [ ClassName "content"] [
        p [] [ str "Load and correct one wav file's worth of speech alignment data at a time. Click on the cat in the corner for more information." ]
        //https://jsfiddle.net/chintanbanugaria/uzva5byy/
        //OnChange (fun ev -> !!ev.target?value |> ChangeStr |> dispatch )
        Fulma.File.file [ 
          Fulma.File.HasName
          File.Props [ OnChange (fun ev ->  UpdateWavPath (ev.target?files |> unbox<Browser.Types.FileList>) |> dispatch ) ] 
          // File.Props [ OnChange (fun ev ->  UpdateWavPath (ev.target?value) |> dispatch ) ] 
          ] [ 
          Fulma.File.label [ ] [ 
            Fulma.File.input [ ]
            Fulma.File.cta [ ] [ 
              Fulma.File.icon [ ] [ 
                Icon.icon [ ] [ 
                  // i [ Class "fas fa-upload" ] [] 
                  Fa.i [ Fa.Solid.Upload ] []
                  ]
              ]
              Fulma.File.label [ ] [ str "Choose a file..." ] ]
            Fulma.File.name [ ] [ str model.WavPath ] 
          ] 
        ] 
        Fulma.File.file [ 
          Fulma.File.HasName 
          File.Props [ OnChange (fun ev ->  UpdateJsonPath (ev.target?files |> unbox<Browser.Types.FileList>) |> dispatch ) ] 
          ] [ 
          Fulma.File.label [ ] [ 
            Fulma.File.input [ ]
            Fulma.File.cta [ ] [ 
              Fulma.File.icon [ ] [ 
                Icon.icon [ ] [ 
                  // i [ Class "fas fa-upload" ] [] 
                  Fa.i [ Fa.Solid.Upload ] []
                  ]
              ]
              Fulma.File.label [ ] [ str "Choose a file..." ] ]
            Fulma.File.name [ ] [ str model.JsonPath ] 
          ] 
          
        ] 
        Button.button [ Button.Color IsPrimary ]
                    [ str "Submit" ] 
        h2 [] [ str "Keymapping" ]
        ul [] [
          li [] [ str "keymapping 1" ]
          li [] [ str "keymapping 2" ]
        ]
        p [] [ str "Text" ] 
        textarea [
                    ClassName "input"
                    DefaultValue model.Data.[model.Index].Text
                    Size 100.0
                    Style [
                        Width "100%"
                        Height "75px"
                    ] 
                    OnInput (fun ev ->  UpdateText (ev.target?value) |> dispatch )
                ] []

        p [] [ str "Text with numerics expanded to words" ] 
        textarea [
                    ClassName "input"
                    DefaultValue model.Data.[model.Index].ExpandedText
                    Size 100.0
                    Style [
                        Width "100%"
                        Height "75px"
                    ] 
                    OnInput (fun ev ->  UpdateExpandedText (ev.target?value) |> dispatch )
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
