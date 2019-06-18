# Manual Subtitle Speech Alignment

This repository extends the automated alignment work described in [aolney/SouthParkTTSData](https://github.com/aolney/SouthParkTTSData).

Some of the data produced by the aforementioned work suffers from the following defects:

- Music/noise in the background
- Overlapping speech
- Clipped final word (e.g. ~100ms)
- Preceeding/following silence (rare)
- Wrong speaker (rare)

To resolve these, we create a UI with data preview/edit capabilities, similar to [finetuneas](https://github.com/ozdefir/finetuneas).
However, unlike that work, we:

- Allow editing the transcript (i.e., we do not assume it is correct)
- Allow rejection of utterances entirely, with rejection codes
- Support a keyboard-oriented UI for faster review/correction

The input is required to be wav audio and json with the following format, where the times are in milliseconds:
``` javascript
[
  {
    "Start": 184170,
    "Stop": 184284,
    "Text": "YES, HE CAN!"
  }
]
```
A live demonstration [is available here.](https://olney.ai/manual-subtitle-speech-alignment)

**TODO VIDEO DEMO**

------------------------------

## Development requirements

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.1 or higher
* [node.js](https://nodejs.org) with [npm](https://www.npmjs.com/)
* An F# editor like Visual Studio, Visual Studio Code with [Ionide](http://ionide.io/) or [JetBrains Rider](https://www.jetbrains.com/rider/).

## Building and running the app

* Install JS dependencies: `npm install`
* Install .NET dependencies: `mono .paket/paket.exe install`
* Start Webpack dev server: `npx webpack-dev-server` or `npm start`
* After the first compilation is finished, in your browser open: http://localhost:8080/

Any modification you do to the F# code will be reflected in the web page after saving.

## Building and running the tests

* Build: `npm pretest`
* Run: `npm test`

## Project structure

### npm/yarn

JS dependencies are declared in `package.json`, while `package-lock.json` is a lock file automatically generated.

### paket

[Paket](https://fsprojects.github.io/Paket/) 

> Paket is a dependency manager for .NET and mono projects, which is designed to work well with NuGet packages and also enables referencing files directly from Git repositories or any HTTP resource. It enables precise and predictable control over what packages the projects within your application reference.

.NET dependencies are declared in `paket.dependencies`. The `src/paket.references` lists the libraries actually used in the project. Since you can have several F# projects, we could have different custom `.paket` files for each project.

Last but not least, in the `.fsproj` file you can find a new node: `	<Import Project="..\.paket\Paket.Restore.targets" />` which just tells the compiler to look for the referenced libraries information from the `.paket/Paket.Restore.targets` file.

### Fable-splitter

[Fable-splitter]() is a standalone tool which outputs separated files instead of a single bundle. Here all the js files are put into the `test-build` or `node-build`  dir. And the main entry point is our `App.js` file.

### Webpack

[Webpack](https://webpack.js.org) is a JS bundler with extensions, like a static dev server that enables hot reloading on code changes. Fable interacts with Webpack through the `fable-loader`. Configuration for Webpack is defined in the `webpack.config.js` file. Note this sample only includes basic Webpack configuration for development mode, if you want to see a more comprehensive configuration check the [Fable webpack-config-template](https://github.com/fable-compiler/webpack-config-template/blob/master/webpack.config.js). Deployment uses Webpack to populate the `deploy` directory and then pushes that directory to `gh-pages`.

### Web assets

The `index.html` file and other assets like an icon can be found in the `public` folder.