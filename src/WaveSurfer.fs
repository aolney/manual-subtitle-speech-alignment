module WaveSurfer

open System
open Fable.Core
open Browser
open Fable.Core.JS

type [<AllowNullLiteral>] WaveSurfer =
    abstract load: path: obj -> unit
    abstract loadBlob: blob: obj -> unit
    abstract play: start:float * stop:float -> unit
    abstract on: label:string * callback: (obj -> unit) -> unit
    abstract create: configuration: obj -> WaveSurfer
    abstract getCurrentTime : unit -> float
    abstract getDuration: unit -> float
    abstract seekAndCenter: zeroToOne: float -> unit
    abstract seekTo: zeroToOne: float -> unit
    abstract skipBackward: seconds: float -> unit
    abstract toggleScroll: unit -> unit
    abstract zoom: pixelsPerSecond : int -> unit
    abstract setHeight: pixels: int -> unit
    //TODO not sure how to import region plugin; this is not working now
//     abstract addRegion: options:obj -> obj
//     abstract clearRegions: unit -> unit
    
// type [<AllowNullLiteral>] Regions =
//     abstract create: configuration: obj -> Regions
//     abstract add: params: obj -> obj
//     abstract init: wavesurfer: WaveSurfer -> unit
//     abstract clear: params: unit -> unit

//Link to JS
[<Import("*","wavesurfer.js")>]
let WaveSurfer : WaveSurfer = jsNative


// [<Import("*","wavesurfer/plugin/wavesurfer.regions")>]
// let Regions : Regions = jsNative
