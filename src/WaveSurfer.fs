module WaveSurfer

open System
open Fable.Core
open Browser
open Fable.Core.JS

type [<AllowNullLiteral>] WaveSurfer =
    abstract load: path: obj -> unit
    abstract loadBlob: blob: obj -> unit
    abstract play: start:float * stop:float -> unit
    abstract on: label:string * callback: (unit -> unit) -> unit
    abstract create: configuration: obj -> WaveSurfer
    abstract getCurrentTime : unit -> unit
    
// and [<AllowNullLiteral>] WaveSurferStatic =
//     [<Emit("WaveSurfer.create($0)")>] 
//     abstract Create: configuration: obj -> WaveSurfer

// //Link to JS
// [<Import("*","wavesurfer")>]
// let WaveSurfer : WaveSurferStatic = jsNative

//Link to JS
[<Import("*","wavesurfer")>]
let WaveSurfer : WaveSurfer = jsNative
