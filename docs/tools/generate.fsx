// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "FSharpComposableQuery.dll" ]
// Web site location for the generated documentation
let website = "http://fsprojects.github.io/FSharp.Linq.ComposableQuery"

// Specify more information about your project
let info =
  [ "project-name", "FSharpComposableQuery"
    "project-author", "Your Name"
    "project-summary", "A short summary of your project"
    "project-github", "https://github.com/fsharp/FSharpComposableQuery"
    "project-nuget", "https://www.nuget.org/packages/FSharpComposableQuery" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#I "../../packages/build/FSharp.Formatting/lib/net40"
#r "../../packages/build/FAKE/tools/FakeLib.dll"
#r "../../packages/build/FSharpVSPowerTools.Core/lib/net45/FSharpVSPowerTools.Core.dll"
#r "../../packages/build/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
#r "System.Web.Razor.dll"
#r "RazorEngine.dll"
#r "FSharp.Literate.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.MetadataFormat.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ </> "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ </> "../../bin"
let content    = __SOURCE_DIRECTORY__ </> "../content"
let output     = __SOURCE_DIRECTORY__ </> "../output"
let files      = __SOURCE_DIRECTORY__ </> "../files"
let templates  = __SOURCE_DIRECTORY__ </> "templates"
let formatting = __SOURCE_DIRECTORY__ </> "../../packages/build/FSharp.Formatting/"
let docTemplate = formatting </> "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRoots =
  [ templates; formatting </> "templates"
    formatting </> "templates/reference" ]

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output </> "styles")
  CopyRecursive (formatting </> "styles") (output </> "content") true 
    |> Log "Copying styles and scripts: "

// Build API reference from XML comments
let buildReference () =
  CleanDir (output </> "reference")
  for lib in referenceBinaries do
    MetadataFormat.Generate
      ( bin </> lib, output </> "reference", layoutRoots, 
        parameters = ("root", root)::info )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output </> sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots )

// Generate
copyFiles()
buildDocumentation()
buildReference()
