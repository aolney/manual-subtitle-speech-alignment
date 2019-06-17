module App

open Fable.Core
open Fable.Core.JsInterop
//open Fable.Import
//open Fable.Import.Browser
open Browser
open Browser.Types
open Browser.Url
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
open Fable.Import
open System.Runtime.CompilerServices
open Fable.React.ReactiveComponents
// open System.UIntPtr

//open Elmish.Browser.Navigation
//open Elmish.Browser.UrlParser

// importAll "../sass/main.sass"

//Fable 2 transition
let inline toJson x = Encode.Auto.toString(4, x)
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

type Mode = | TextEdit | Coding | Loading
type Model = 
  {
    Mode : Mode
    WavFile : Browser.Types.File option
    JsonFile : Browser.Types.File option
    Index : int
    Data : Datum // Datum[]
  }

type Msg =
    | UpdateText of string
    | UpdateExpandedText of string
    | UpdateWavFile of Browser.Types.FileList
    | UpdateJsonFile of Browser.Types.FileList
    // | LoadFiles
    | DecodeJson of string
    | PlayWav
    | PlayWavFinished
    | WaveSurferReady
    | UpdateStart of string
    | UpdateStop of string
    // | KeyUp of float
    | KeyDown of float

/// Mapped to functions for increased speed
module Keys =
    let [<Literal>] Tab = 9.
    let [<Literal>] Enter = 13. //go to next datum
    let [<Literal>] Ctrl = 17.
    let [<Literal>] Alt = 18.
    //use as a mode shift 
    let [<Literal>] Escape = 27.
    let [<Literal>] Left = 37.
    let [<Literal>] Up = 38. //go to previous datum
    let [<Literal>] Right = 39.
    let [<Literal>] Down = 40. //go to next datum
    //keys for adjusting times
    let [<Literal>] D = 68. //shift start earlier
    let [<Literal>] F = 70. //shift start later
    let [<Literal>] J = 74. //shift stop earlier
    let [<Literal>] K = 75. //shift stop later
    //keys for coding problems
    let [<Literal>] M = 77. //music
    let [<Literal>] N = 78. //noise
    let [<Literal>] O = 79. //overlapping speech
    let [<Literal>] W = 87. //wrong character
    let [<Literal>] X = 88. //other problem
    //keys for review
    let [<Literal>] P = 80. //play entire clip

let subscribeToKeyEvents dispatch =
    window.addEventListener("keydown", fun ev ->
        KeyDown (ev :?> KeyboardEvent).keyCode  |> dispatch )
    // window.addEventListener("keyup", fun ev ->
    //     KeyUp (ev :?> KeyboardEvent).keyCode  |> dispatch )


let init () : Model * Cmd<Msg> =
  ( { 
      Mode = Coding
      WavFile = None//"*.wav"
      JsonFile = None//"*.json"
      Index = 0; 
      Data = //[|   
        {
          Start= 0
          Stop= 0
          Text= ""
          Id= ""
          WavFile= ""
          FrontAligned= false
          EndAligned= false
          SumAlignmentDiff= 0
          ProportionAligned= 0.
          ExpandedText= ""
        }
      //|] 
    }, [subscribeToKeyEvents] )

//Globals
// We have html elements in index.html to avoid handling them through react

//React not responding to changes in array when in model, also copying so much data is slow
let mutable data = Array.empty<Datum>

let wavesurfer = 
  WaveSurfer.create(
     createObj [
      "container" ==> "#waveform"
      "waveColor" ==> "violet"
      "progressColor" ==> "purple"
    //   "plugins" ==> [
    //     WaveSurfer.Regions.create( createObj[] )
    // ]
  ]
)

// Update
// ---------------------------------------
// wavesurfer.on("ready", fun _ -> 
//   // wavesurfer.setHeight(500)
//   // RegionsPlugin.init(wavesurfer)
//   wavesurfer.zoom(300) 
//   Browser.Dom.console.log( "wavesurfer ready" )
//   )
// loads much faster than graphic appears
wavesurfer.on("loading", fun percent -> 
  Browser.Dom.console.log( percent )
)

let seekWavesurfer( desiredMilliseconds: int ) =
   wavesurfer.seekTo( float(desiredMilliseconds) / 1000.0 / wavesurfer.getDuration() );

let seekCenterWavesurfer( desiredMilliseconds: int ) =
   wavesurfer.seekAndCenter( float(desiredMilliseconds) / 1000.0 / wavesurfer.getDuration() );

// let addWavesurferRegion(startMillseconds : int) (stopMilliseconds: int) =
//   wavesurfer.addRegion(
//     createObj [
//       "start" ==> float(startMillseconds) / 1000.0
//       "end" ==> float(stopMilliseconds) / 1000.0
//     ]
//   )
let update msg (model:Model) =
  match msg with
  | UpdateText(input) ->

    //react not firing with this?
    // model.Data.[model.Index] <- { model.Data.[model.Index] with Text = input}
    ( model, [])
  | UpdateExpandedText(input) ->

    //react not firing with this?
    // model.Data.[model.Index] <- { model.Data.[model.Index] with ExpandedText = input}
    ( model, [])
  | UpdateWavFile(input) -> 
    let wavesurferLoadCommand dispatch =
        wavesurfer.on("ready", fun _ -> WaveSurferReady |> dispatch )
    wavesurfer.loadBlob( input.[0] )
    ( {model with WavFile = Some(input.[0]); Mode=Loading}, [wavesurferLoadCommand])
  | UpdateJsonFile(input) -> 
    let fileReadCommand dispatch =
      let fileReader = Browser.Dom.FileReader.Create ()
      fileReader.onload <- fun _ -> fileReader.result |> unbox<string> |> DecodeJson |> dispatch
      fileReader.readAsText input.[0]
    ( {model with JsonFile = Some(input.[0])}, [fileReadCommand] )
  //TODO: this is now redundant if we prefer loading when the file is selected
  // | LoadFiles ->
  //   match model.WavFile, model.JsonFile with
  //   | Some(wavFile),Some(jsonFile) -> 
  //     //let wavUrl = URL.createObjectURL(wavFile)
  //     wavesurfer.loadBlob( wavFile )
  //     let wavesurferLoadCommand dispatch =
  //       wavesurfer.on("ready", fun _ -> WaveSurferReady |> dispatch )
  //     //let jsonUrl = URL.createObjectURL(jsonFile)
  //     let fileReadCommand dispatch =
  //       let fileReader = Browser.Dom.FileReader.Create ()
  //       fileReader.onload <- fun _ -> fileReader.result |> unbox<string> |> DecodeJson |> dispatch
  //       fileReader.readAsText jsonFile
  //     ( model, [fileReadCommand;wavesurferLoadCommand])
  //   | _,_ -> ( model, [])
  | DecodeJson(input) ->
    //let data = input |> ofJson<Datum[]> 
    //data now global
    data <- input |> ofJson<Datum[]> 
    ( {model with Index=0; Data=data.[0] }, [])
  | WaveSurferReady -> 
    // wavesurfer.on("ready", fun _ -> 
    // wavesurfer.setHeight(500)
    // RegionsPlugin.init(wavesurfer)}}
    wavesurfer.zoom(300) 
    // Browser.Dom.console.log( "wavesurfer ready" )
    ( {model with Mode=Coding}, [])
  | PlayWav ->
    // wavesurfer.zoom(100)
    wavesurfer.play ( float(model.Data.Start) / 1000.0 , float(model.Data.Stop) / 1000.0 )
    let postPlayCommand dispatch = wavesurfer.on("pause", fun _ -> 
      Browser.Dom.console.log( "play finished" )
      PlayWavFinished |> dispatch )
    ( model, [postPlayCommand])
  | PlayWavFinished ->
    seekWavesurfer( model.Data.Start )
    // wavesurfer.skipBackward( float(model.Data.Stop - model.Data.Start)/1000.0 )
    ( model, [])
  | UpdateStart(input) ->
    let data = { model.Data with Start=System.Int32.Parse(input) }
    ( {model with Data=data }, [])
  | UpdateStop(input) ->
    let data = { model.Data with Stop=System.Int32.Parse(input) }
    ( {model with Data=data }, [])
  // | KeyUp code ->
  //   match code with
  //   | Keys.Left -> model,[]
  //   | _ -> model,[]
  | KeyDown code ->
    match code, model.Mode with
    | Keys.Escape,_ -> 
      match model.Mode with
      | Coding -> { model with Mode=TextEdit},[]
      | TextEdit -> { model with Mode=Coding},[]
      | _ -> (model,[])
    | Keys.P, Coding ->
      //wavesurfer.zoom(300)
      //seekWavesurfer( data.[model.Index + 1].Start )
      // wavesurfer.toggleScroll()
      // seekWavesurfer( model.Data.Start )
      wavesurfer.play ( float(model.Data.Start) / 1000.0 , float(model.Data.Stop) / 1000.0 )
      model,[]
    | Keys.Up,_ -> 
      let newModel =
        if model.Index > 0 then
          // wavesurfer.clearRegions()
          let nextDatum = data.[model.Index - 1]
          // addWavesurferRegion nextDatum.Start nextDatum.Stop |> ignore
          seekCenterWavesurfer( nextDatum.Start )
          { model with Index = model.Index - 1; Data = nextDatum }
        else
          model
      ( newModel,[] )
    | Keys.Down,_ -> 
      let newModel =
        if model.Index < data.Length - 1 then
          // wavesurfer.clearRegions()
          let nextDatum = data.[model.Index + 1]
          // addWavesurferRegion nextDatum.Start nextDatum.Stop |> ignore
          seekCenterWavesurfer( nextDatum.Start )
          { model with Index = model.Index + 1; Data = nextDatum }
        else
          model
      ( newModel,[] )
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
  // div [ ClassName "columns is-vcentered" ] [ 
  //   div [ ClassName "column" ] [ 
  Section.section [] [
    //spinner defined in sass
    // div [ ClassName "loading"; Hidden ( match model.Mode with | Loading -> true | _ -> false )  ] []
    div [ ClassName "loading"; Hidden ( model.Mode = Mode.Coding || model.Mode = Mode.TextEdit )  ] []
    //div [ ClassName "loading"; Hidden true  ] []
    Container.container [ Container.IsFluid ] [
      // Section.section [] [
        //h1 [ ClassName "title"] [ str "Manual Subtitle Speech Alignment"]
      Heading.h2 [ ] [ str "Manual Subtitle Speech Alignment"]
      Content.content [ ] [
        p [] [ str "Load and correct one wav file's worth of speech alignment data at a time. Click on the cat in the corner for more information." ]
      ]
      Fulma.Columns.columns [] [
        Fulma.Columns.columns [ Columns.IsCentered ] [
          Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
            Fulma.File.file [ 
              Fulma.File.HasName
              File.Props [ OnChange (fun ev ->  UpdateWavFile (ev.target?files |> unbox<Browser.Types.FileList>) |> dispatch ) ] 
              ] [ 
              Fulma.File.label [ ] [ 
                Fulma.File.input [ Props [ Accept ".wav"] ] //audio/*
                Fulma.File.cta [ ] [ 
                  Fulma.File.icon [ ] [ 
                    Icon.icon [ ] [  
                      Fa.i [ Fa.Solid.Upload ] []
                      ]
                  ]
                  Fulma.File.label [ ] [ str "Choose a file..." ] ]
                Fulma.File.name [ ] [ str (match model.WavFile with | Some(file) -> file.name | None -> "*.wav" ) ] 
              ] 
            ]
          ]
        ]
        Fulma.Column.column [ 
          Column.Width  (Screen.All, Column.IsThreeFifths )  
          Column.Modifiers [ 
            Modifier.TextAlignment (Screen.All, TextAlignment.Centered) 
            Modifier.TextColor IsPrimary 
            ]
          ] [
          str ( match model.Mode with | TextEdit -> "Text entry mode. Key commands disabled. Press Esc to switch." | Coding -> "Coding mode. Key commands enabled. Press Esc to switch." | Loading -> "Loading wav. Please be patient") 
        ]
        Fulma.Columns.columns [ Columns.IsCentered  ] [
          Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
            Fulma.File.file [ 
              Fulma.File.HasName 
              File.Props [ OnChange (fun ev ->  UpdateJsonFile (ev.target?files |> unbox<Browser.Types.FileList>) |> dispatch ) ] 
              ] [ 
              Fulma.File.label [ ] [ 
                Fulma.File.input [ Props [ Accept ".json" ]]
                Fulma.File.cta [ ] [ 
                  Fulma.File.icon [ ] [ 
                    Icon.icon [ ] [ 
                      Fa.i [ Fa.Solid.Upload ] []
                      ]
                  ]
                  Fulma.File.label [ ] [ str "Choose a file..." ] ]
                Fulma.File.name [ ] [ str (match model.JsonFile with | Some(file) -> file.name | None -> "*.json" ) ] 
              ] 
            ] 
          ]
        ]
      ]
      //editing 
      Fulma.Columns.columns [] [
        Fulma.Columns.columns [ Columns.IsCentered ] [
          Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
            p [] [ str "Start time (milliseconds)" ] 
            Input.text [
                  Input.Color IsPrimary
                  Input.IsRounded
                  Input.Value ( model.Data.Start.ToString() )
                  Input.Props [ OnChange (fun ev ->  !!ev.target?value |> UpdateStart|> dispatch ) ]
                ]
          ]
        ]
        
        Fulma.Column.column [ Column.Width  (Screen.All, Column.IsThreeFifths )  ] [
          str "Text"
          textarea [
            ClassName "input"
            // DefaultValue model.Data.[model.Index].Text
            Value model.Data.Text
            Size 100.0
            Style [
                Width "100%"
                Height "75px"
            ] 
            OnChange (fun ev ->  !!ev.target?value |> UpdateText|> dispatch )
          ] []
     
          str "Text with numerics expanded to words" 
          textarea [
            ClassName "input"
            // DefaultValue model.Data.[model.Index].ExpandedText
            Value model.Data.ExpandedText
            Size 100.0
            Style [
                Width "100%"
                Height "75px"
            ] 
            OnChange (fun ev ->  !!ev.target?value |> UpdateExpandedText |> dispatch )
          ] []
        ]
        Fulma.Columns.columns [ Columns.IsCentered ] [
          Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
            p [] [ str "Stop time (milliseconds)" ]
            Input.text [
              Input.Color IsPrimary
              Input.IsRounded
              Input.Value ( model.Data.Stop.ToString() )
              Input.Props [ OnChange (fun ev ->  !!ev.target?value |> UpdateStop|> dispatch ) ]
            ]
          ]
        ]
      ]
           //landing place for now
      // Container.container [] [
      // Button.button [ 
      //   Button.Color IsPrimary
      //   Button.OnClick (fun _ -> dispatch LoadFiles)
      //   ] [ str "Load Files" ]
      // Button.button [ 
      //   Button.Color IsGreyLight
      //   Button.OnClick (fun _ -> dispatch PlayWav)
      //   ] [ str "Play Wav" ]
      // Button.button [ 
      //   Button.Color IsBlack
      //   Button.OnClick (fun _ -> Browser.Dom.console.log( wavesurfer.getCurrentTime() ) )
      //   ] [ str "Get time" ]
      //Browser.Dom.console.log( wavesurfer.getCurrentTime() )
      // h2 [] [ str "Keymapping" ]
      // ul [] [
      //   li [] [ str "keymapping 1" ]
      //   li [] [ str "keymapping 2" ]
      // ]
      // ]
      //debuggy but also generally useful
      pre [  Style [FontSize 10.0 ] ] [ str (model.Data |> toJson) ]
    ]  
  ]

// App
Program.mkProgram init update view
//|> Program.toNavigable (parseHash pageParser) urlUpdate
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
//|> Program.withHMR
#endif
|> Program.withReactBatched "elmish-app" 
|> Program.run
