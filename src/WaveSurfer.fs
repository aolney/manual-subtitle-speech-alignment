module WaveSurfer

open System
open Fable.Core
open Browser
open Fable.Core.JS

type [<AllowNullLiteral>] WaveSurfer =
    abstract load: path: obj -> unit
    abstract loadBlob: blob: obj -> unit
    abstract play: start:float * stop:float -> unit
    abstract pause: unit -> unit
    abstract stop: unit -> unit
    abstract isPlaying: unit -> bool
    abstract on: label:string * callback: (obj -> unit) -> unit
    abstract create: configuration: obj -> WaveSurfer
    abstract setCurrentTime : float -> unit
    abstract getCurrentTime : unit -> float
    abstract getDuration: unit -> float
    abstract seekAndCenter: zeroToOne: float -> unit
    abstract seekTo: zeroToOne: float -> unit
    abstract skipBackward: seconds: float -> unit
    abstract toggleScroll: unit -> unit
    abstract zoom: pixelsPerSecond : int -> unit
    abstract setHeight: pixels: int -> unit
    abstract addPlugin : configuration: obj -> WaveSurfer
    abstract initPlugin: name:string -> WaveSurfer
    //TODO not sure how to import region plugin; this is not working now
    abstract addRegion: options:obj -> obj
    abstract clearRegions: unit -> unit
    
type [<AllowNullLiteral>] RegionsPlugin =
    abstract create: configuration: obj -> RegionsPlugin
    abstract addRegion: parameters: obj -> obj
    abstract clearRegions: parameters: unit -> unit
type [<AllowNullLiteral>] Region =
    abstract play: unit -> unit
    abstract playLoop: unit -> unit
    abstract remove: unit -> unit


//Link to JS
[<Import("*","wavesurfer.js")>]
let WaveSurfer : WaveSurfer = jsNative


[<Import("*","../node_modules/wavesurfer.js/dist/plugin/wavesurfer.regions.min.js")>]
let RegionsPlugin : RegionsPlugin = jsNative
