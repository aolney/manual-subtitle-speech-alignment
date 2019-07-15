module App

open Fable.Core
open Fable.Core.JsInterop

open Browser
open Browser.Types
open Browser.Url

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Elmish.Debug
open Elmish.HMR
open Thoth.Json

open Fulma
open Fable.FontAwesome
open Fable.FontAwesome.Free

open WaveSurfer
open System.IO
open Fable.Import
open System.Runtime.CompilerServices
open Fable.React.ReactiveComponents
open Fable.Import
open System.Text
open System

//Fable 2 transition
let inline toJson x = Encode.Auto.toString(4, x)
let inline ofJson<'T> json = Decode.Auto.unsafeFromString<'T>(json)


// Todo: tests
let randomFeature() = [1;2;3]

// Domain
// ---------------------------------------
/// Rather specific to https://github.com/aolney/SouthParkTTSData , but only Start/Stop/Text are required fields
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
    Status : string //because this is not in our original data, we need a custom decoder with default value
  }
  static member Decoder : Decoder<Datum> =
      Decode.object
          (fun get ->
              {
                Start = get.Required.Field "Start" Decode.int
                Stop = get.Required.Field "Stop" Decode.int
                Text = get.Required.Field "Text" Decode.string
                //everything else is optional
                ExpandedText = get.Optional.Field "ExpandedText" Decode.string |> Option.defaultValue ""
                Id = get.Optional.Field "Id" Decode.string |> Option.defaultValue ""
                WavFile = get.Optional.Field "WavFile" Decode.string |> Option.defaultValue ""
                FrontAligned = get.Optional.Field "FrontAligned" Decode.bool |> Option.defaultValue false
                EndAligned = get.Optional.Field "EndAligned" Decode.bool |> Option.defaultValue false
                SumAlignmentDiff = get.Optional.Field "SumAlignmentDiff" Decode.int |> Option.defaultValue 0
                ProportionAligned = get.Optional.Field "ProportionAligned" Decode.float |> Option.defaultValue 0.
                Status = get.Optional.Field "Status" Decode.string |> Option.defaultValue ""
              }
          )

type Mode = | TextEdit | Coding | Loading
type Model = 
  {
    Mode : Mode
    WavFile : Browser.Types.File option
    JsonFile : Browser.Types.File option
    Index : int
    Datum : Datum
  }

type Msg =
    | UpdateText of string
    | UpdateExpandedText of string
    | UpdateWavFile of Browser.Types.FileList
    | UpdateJsonFile of Browser.Types.FileList
    | Download
    | DecodeJson of string
    | PlayWav
    | WaveSurferReady
    | UpdateStart of string
    | UpdateStop of string
    | KeyDown of float

/// Mapped to functions for increased speed; some of these aren't used
module Keys =
    let [<Literal>] Tab = 9.
    let [<Literal>] Enter = 13. //go to next datum
    let [<Literal>] Ctrl = 17.
    let [<Literal>] Alt = 18.
    let [<Literal>] Escape = 27. //use as mode shift
    let [<Literal>] Space = 32. //also play wav file
    let [<Literal>] Left = 37.
    let [<Literal>] Up = 38. //go to previous datum
    let [<Literal>] Right = 39.
    let [<Literal>] Down = 40. //go to next datum
    let [<Literal>] B = 66. //bug background noises (South Park specific)
    let [<Literal>] C = 67. //make a copy (for splitting a record)
    //keys for adjusting times
    let [<Literal>] D = 68. //shift start earlier
    let [<Literal>] F = 70. //shift start later
    let [<Literal>] J = 74. //shift stop earlier
    let [<Literal>] K = 75. //shift stop later
    //mark as good
    let [<Literal>] G = 71.
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
        
let init () : Model * Cmd<Msg> =
  ( { 
      Mode = Coding
      WavFile = None//"*.wav"
      JsonFile = None//"*.json"
      Index = 0; 
      Datum =
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
          Status = ""
        }
    }, [subscribeToKeyEvents] )

//Globals
// wavesurfer container is in html to avoid handling it through react; we programmatically initialize here
let wavesurfer = 
  WaveSurfer.create(
     createObj [
      "container" ==> "#waveform"
      "waveColor" ==> "violet"
      "progressColor" ==> "purple"
      "barHeight" ==> 10
      "minPxPerSec" ==> 400
      "forceDecode" ==> true
      "scrollParent" ==> true
    //  This fails unexpectedly, so we use alternative API when wavesurfer fires ready
    //   "plugins" ==> [
    //     RegionsPlugin.create(  )
    // ]
  ]
)

// Only keeping datum being edited in model at one time; copying all data is slow
let data = ResizeArray<Datum>()

//This could be in model if we made it a user option
let timeIncrement = 10 //milliseconds

// Update
// ---------------------------------------
let seekWavesurfer( desiredMilliseconds: int ) =
   wavesurfer.seekTo( float(desiredMilliseconds) / 1000.0 / wavesurfer.getDuration() );

let seekCenterWavesurfer( desiredMilliseconds: int ) =
   wavesurfer.seekAndCenter( float(desiredMilliseconds) / 1000.0 / wavesurfer.getDuration() );

let updateWavesurferRegion( datum: Datum ) =
  wavesurfer.clearRegions()
  wavesurfer.addRegion(
    createObj [
      "start" ==> float(datum.Start) / 1000.0
      "end" ==> float(datum.Stop) / 1000.0
      "color" ==>  "rgba(0, 0, 0, 0.3)" 
    ]
  ) |> ignore //returns the region, but we ignore it

let boundedTimeShift newTime =
  if newTime < 0 then
    0
  else if newTime > int(wavesurfer.getDuration() * 1000.0) then
    int(wavesurfer.getDuration() * 1000.0)
  else
    newTime

let nextValidDatumIndex startIndex step =
  let mutable index = startIndex + step
  let mutable notFound = true
  while notFound do
    //out of bounds set to 0 (even if 0 is filtered)
    if index < 0 then
      index <- 0
      notFound <- false
    //out of bounds set to max (even if max is filtered)
    else if 
      index >= data.Count then
      index <- data.Count - 1
      notFound <- false
    //in bounds, filtered, advance
    else if
      data.[index].Status = "automaticallyfiltered" then
      index <- index + step
    //in bounds, regular, stop
    else
      notFound <- false
  //
  index

let update msg (model:Model) =
  match msg with

  | UpdateText(input) ->
    if model.Mode = TextEdit then
      let datum = { model.Datum with Text = input }
      ( {model with Datum=datum}, [])
    else
      ( model, [] )

  | UpdateExpandedText(input) ->
    if model.Mode = TextEdit then
      let datum = { model.Datum with ExpandedText = input }
      ( {model with Datum=datum}, [])
    else
      ( model, [])

  | UpdateWavFile(input) -> 
    (document.activeElement :?> HTMLElement).blur() //prevent "enter" from relaunching dialog
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
    
  | DecodeJson(input) ->
    (document.activeElement :?> HTMLElement).blur() //prevent "enter" from relaunching dialog
    data.Clear()
    data.AddRange(
        input 
        |> Decode.unsafeFromString ( Thoth.Json.Decode.array Datum.Decoder ) 
        //deal with bad data; here just invalid start/stop times
        |> Array.map( fun d -> 
          if d.Start > d.Stop then 
            let center = (d.Start + d.Stop) / 2
            { d with Start = center - 2000; Stop = center + 2000}
          else
            d
        )) |> ignore
    let index = nextValidDatumIndex -1 1
    ( {model with Index=index; Datum=data.[index] }, [])

  | WaveSurferReady -> 
    wavesurfer.addPlugin( RegionsPlugin.create() ) |> ignore
    wavesurfer.initPlugin( "regions" ) |> ignore
    ( {model with Mode=Coding}, [])

  | PlayWav ->
    wavesurfer.play ( float(model.Datum.Start) / 1000.0 , float(model.Datum.Stop) / 1000.0 )
    ( model, [])

  | UpdateStart(input) ->
    let datum = { model.Datum with Start=System.Int32.Parse(input) }
    ( {model with Datum=datum }, [])

  | UpdateStop(input) ->
    let datum = { model.Datum with Stop=System.Int32.Parse(input) }
    ( {model with Datum=datum }, [])

  | KeyDown code ->
    match code, model.Mode with
    //Switch modes
    | Keys.Escape,_ -> 
      (document.activeElement :?> HTMLElement).blur() //clear focus
      match model.Mode with
      | Coding -> { model with Mode=TextEdit},[]
      | TextEdit -> { model with Mode=Coding},[]
      | _ -> (model,[])
    //Play wav clip
    | Keys.P, Coding 
    | Keys.Space, Coding ->
      wavesurfer.play ( float(model.Datum.Start) / 1000.0 , float(model.Datum.Stop) / 1000.0 )
      model,[]
    //Make a copy of this record and insert in place
    | Keys.C, Coding ->
      //duplicate with a unique but derivative id, insert in data, update model
      let newId = model.Datum.Id + "-" + System.DateTime.Now.ToString("yyyyMMddHHmmss")
      let datum = {model.Datum with Id=newId}
      data.Insert(model.Index,datum)
      { model with Datum = datum },[]
    //Shift start earlier
    | Keys.D, Coding ->
      let datum = { model.Datum with Start = boundedTimeShift(model.Datum.Start - timeIncrement)  }
      updateWavesurferRegion datum
      { model with Datum = datum },[]
    //Shift start later
    | Keys.F, Coding ->
      let datum = { model.Datum with Start = boundedTimeShift(model.Datum.Start + timeIncrement)  }
      updateWavesurferRegion datum
      { model with Datum = datum },[]
    //Shift stop earlier
    | Keys.J, Coding ->
      let datum = { model.Datum with Stop = boundedTimeShift(model.Datum.Stop - timeIncrement)  }
      updateWavesurferRegion datum
      { model with Datum = datum },[]
    //Shift stop later
    | Keys.K, Coding ->
      let datum = { model.Datum with Stop = boundedTimeShift(model.Datum.Stop + timeIncrement)  }
      updateWavesurferRegion datum
      { model with Datum = datum },[]
    //Coding status good
    | Keys.G, Coding -> { model with Datum = {model.Datum with Status = "good" }} ,[]
    //Coding status problem: music
    | Keys.M, Coding -> { model with Datum = {model.Datum with Status = "music" }} ,[]
    //Coding status problem: noise
    | Keys.N, Coding -> { model with  Datum = {model.Datum with Status = "noise" }},[]
    //Coding status problem: bug noise
    | Keys.B, Coding -> { model with  Datum = {model.Datum with Status = "crickets" }},[]
    //Coding status problem: overlapping speech
    | Keys.O, Coding -> { model with  Datum = {model.Datum with Status = "overlapping speech" }},[]
    //Coding status problem: wrong character
    | Keys.W, Coding -> { model with  Datum = {model.Datum with Status = "wrong character" }},[]
    //Coding status problem: x-factor
    | Keys.X, Coding -> { model with  Datum = {model.Datum with Status = "other" }},[]        
    | Keys.Up,Coding -> 
      data.[model.Index] <- model.Datum //automatically save current datum to global
      let index = nextValidDatumIndex model.Index -1
      let datum = data.[index]
      updateWavesurferRegion datum
      seekCenterWavesurfer( datum.Start )
      { model with Index = index; Datum = datum },[]
    | Keys.Down,Coding 
    | Keys.Enter,Coding -> 
      if wavesurfer.isPlaying() then wavesurfer.pause()
      data.[model.Index] <- model.Datum //automatically save current datum to global
      let index = nextValidDatumIndex model.Index 1
      let datum = data.[index]
      updateWavesurferRegion datum
      seekCenterWavesurfer( datum.Start )
      { model with Index = index; Datum = datum },[]
    //For all other key commands we do nothing
    | _,_ -> model,[]

  | Download ->
      let a = document.createElement("a") :?> Browser.Types.HTMLLinkElement
      //May need blobs for larger sizes
      //a.href <- URL.createObjectURL( blob );
      a.href <- "data:text/plain;charset=utf-8," + JS.encodeURIComponent( data |> toJson )
      let filename = 
        match  model.JsonFile with
        | Some(file) -> file.name + ".corrected"
        | None -> "empty"
      a.setAttribute("download", filename );
      a.click()
      ( model,[] )
  
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
  Section.section [] [
    //spinner defined in sass
    div [ ClassName "loading"; Hidden ( model.Mode = Mode.Coding || model.Mode = Mode.TextEdit )  ] []
    Container.container [ Container.IsFluid ] [
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
                  Input.Value ( model.Datum.Start.ToString() )
                  Input.Props [ OnChange (fun ev ->  !!ev.target?value |> UpdateStart|> dispatch ) ]
                ]
          ]
        ]
        
        Fulma.Column.column [ Column.Width  (Screen.All, Column.IsThreeFifths )  ] [
          str "Text"
          textarea [
            ClassName "input"
            Value model.Datum.Text
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
            Value model.Datum.ExpandedText
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
              Input.Value ( model.Datum.Stop.ToString() )
              Input.Props [ OnChange (fun ev ->  !!ev.target?value |> UpdateStop|> dispatch ) ]
            ]
          ]
        ]
      ]

      Fulma.Columns.columns [ Columns.IsCentered ] [
        Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
          //debuggy but also generally useful
          pre [  Style [FontSize 10.0 ] ] [ str (model.Datum |> toJson) ]
        ]
        Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
          Button.button [ 
            Button.Color IsPrimary
            Button.OnClick (fun _ -> dispatch Download )
            ] [ str "Get Results" ]
        ]
        Fulma.Column.column [ Column.Width (Screen.All, Column.IsNarrow) ] [
          Dropdown.dropdown [ Dropdown.IsHoverable ]
            [ div [ ]
                [ Button.button [ ]
                    [ span [ ]
                        [ str "Key Command Menu" ]
                      Icon.icon [ Icon.Size IsSmall ]
                        [ Fa.i [ Fa.Solid.AngleDown ]
                            [ ] ] ] ]
              Dropdown.menu [ ]
                [ Dropdown.content [ ]
                    [ Dropdown.Item.a [ ]
                        [ str "Esc -> Change mode" ]
                      Dropdown.Item.a [ ]
                        [ str "Up Arrow -> Previous datum" ]
                      Dropdown.Item.a [ ]
                        [ str "Down Arrow -> Next datum" ]
                      Dropdown.Item.a [ ]
                        [ str "Enter -> Next datum" ]
                      Dropdown.Item.a [ ]
                        [ str "C -> Copy (for splitting)" ]
                      Dropdown.Item.a [ ]
                        [ str "D -> Shift start earlier" ] 
                      Dropdown.Item.a [ ]
                        [ str "F -> Shift start later" ] 
                      Dropdown.Item.a [ ]
                        [ str "J -> Shift stop earlier" ] 
                      Dropdown.Item.a [ ]
                        [ str "K -> Shift stop later" ] 
                      Dropdown.Item.a [ ]
                        [ str "P or Space -> Play datum audio" ] 
                      Dropdown.Item.a [ ]
                        [ str "G -> Status good" ] 
                      Dropdown.Item.a [ ]
                        [ str "M -> Status music problem" ] 
                      Dropdown.Item.a [ ]
                        [ str "N -> Status noise problem" ] 
                      Dropdown.Item.a [ ]
                        [ str "B -> Status noise problem (bugs/crickets)" ] 
                      Dropdown.Item.a [ ]
                        [ str "O -> Status overlapping speech" ] 
                      Dropdown.Item.a [ ]
                        [ str "W -> Status wrong character" ]  
                      Dropdown.Item.a [ ]
                        [ str "X -> Status bad other" ]                                                                                                                                                                                                                         
                        ] ] ]
        ]
      ]
    ]  
  ]

// App
Program.mkProgram init update view
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
//|> Program.withHMR //deprecated???
#endif
|> Program.withReactSynchronous "elmish-app" //batched makes input cursor jump to end of box
|> Program.run
