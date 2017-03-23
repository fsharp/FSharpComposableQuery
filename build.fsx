// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------
#r "System.IO.Compression.FileSystem"
#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System
open System.IO



// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "FSharpComposableQuery"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A Compositional, Safe Query Framework for Dynamic F# Queries."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """
  A Compositional, Safe Query Framework for Dynamic F# Queries
  """
// List of author names (for NuGet package)
let authors = [ "James Cheney"; "Sam Lindley"; "Yordan Stoyanov" ]
// Tags for your project (for NuGet package)
let tags = "F# fsharp LINQ SQL database data dynamic query"

// File system information 
// Pattern specifying all library files (projects or solutions)
let libraryReferences  = !! "src/*/*.fsproj"
// Pattern specifying all test files (projects or solutions)
let testReferences = !! "tests/*/*.fsproj"
// The output directory
let buildDir = "./bin/"
let testBuildDir = "./bin/tests"




// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/fsprojects"
// The name of the project on GitHub
let gitName = "FSharpComposableQuery"

let run cmd args dir =
    ExecProcess (fun info ->
        info.FileName <- cmd
        if not (String.IsNullOrWhiteSpace dir) then
            info.WorkingDirectory <- dir
        info.Arguments <- args
        info.RedirectStandardOutput <- true 
        info.RedirectStandardError <- true
    )  System.TimeSpan.MaxValue |> ignore



// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fileName = "src/" + project + "/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName
      [ Attribute.InternalsVisibleTo "FSharpComposableQuery.Tests"
        Attribute.Title project
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results 


Target "Clean" (fun _ ->
    CleanDirs [
        buildDir 
        testBuildDir 
        "temp" 
        "tests/databases"
    ]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

Target "SetupSqlite" (fun _ ->
//if isWindows && not isMono then
//    let frameworks = ["net40"; "net45"; "net46";"net451"]
//    frameworks
//    |> List.iter (fun fwrk ->
//        let x86source = sprintf "packages/test/System.Data.SQLite.Core/build/%s/x86" fwrk
//        let x64source = sprintf "packages/test/System.Data.SQLite.Core/build/%s/x64" fwrk
        
//        let x64target = sprintf "packages/test/System.Data.SQLite.Core/lib/%s/x64" fwrk
//        let x86target = sprintf "packages/test/System.Data.SQLite.Core/lib/%s/x86" fwrk
        
//        if not (directoryExists x86target) then
//            CopyDir x86target x86source (fun _ -> true)
//        if not (directoryExists x64target) then
//            CopyDir x64target x64source (fun _ -> true)
//    )

    if isMono then 
        let fullPath = Path.GetFullPath
        let sqliteDir = fullPath "sqlite"
        let sqliteVersion = "1.0.104.0"
        let sqliteArchive = sprintf "sqlite-netFx-source-%s.zip" sqliteVersion
        let sqliteSrcUrl  = sprintf "https://system.data.sqlite.org/downloads/%s/sqlite-netFx-source-%s.zip" sqliteVersion sqliteVersion
        let sqliteArchivePath = sqliteDir</>sqliteArchive
        let sqliteSetupDir = sqliteDir</>"Setup"
        let compileInterop = sqliteSetupDir</>"compile-interop-assembly-release.sh"
        let sqliteBin = sqliteDir</>"bin"
        let interopBin = sqliteBin</>"2013"</>"Release"</>"bin"
        let sqliteInteropMono = interopBin</>"libSQLite.Interop.so"
        let testbinDebug = "tests/FSharpComposableQuery.Tests/bin/Debug"
        let testbinRelease = "tests/FSharpComposableQuery.Tests/bin/Release"

        if not (fileExists sqliteInteropMono) then
            ensureDirectory sqliteDir

            if not (fileExists sqliteArchivePath) then 
                use webclient = new System.Net.WebClient()
                webclient.DownloadFile (sqliteSrcUrl, sqliteDir)
            
            Compression.ZipFile.ExtractToDirectory (sqliteArchivePath, sqliteDir)
            
            run "chmod" ("+x "+compileInterop) sqliteSetupDir
            run compileInterop "" sqliteSetupDir

            (directoryInfo sqliteBin).EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories)
            |> Seq.iter (fun (fi:FileInfo) -> printfn "  - %s" fi.FullName)

            ensureDirectory testbinDebug
            ensureDirectory testbinRelease

        CopyFile "packages/test/Mono.Data.SQLite.Portable" sqliteInteropMono 
        CopyFile testbinDebug sqliteInteropMono
        CopyFile testbinRelease sqliteInteropMono
)


// --------------------------------------------------------------------------------------
// Build library

Target "Build" (fun _ ->
//    let props = [("DocumentationFile", project + ".XML")]   //explicitly generate XML documentation
    MSBuildRelease buildDir "Build" libraryReferences
    |> Log "Build-Output: "
)

// --------------------------------------------------------------------------------------
// Build tests and library

Target "BuildTest" (fun _ ->
    MSBuildDebug "" "Rebuild" testReferences
    |> Log "BuildTest-Output: "
)

// --------------------------------------------------------------------------------------
// Run unit tests using test runner & kill test runner when complete
open Fake.Testing
// Pattern specifying assemblies to be tested
let testAssemblies = !! "tests/FSharpComposableQuery.Tests/bin/Debug/FSharpComposableQuery*Tests*.dll"

Target "RunTests" (fun _ ->

    testAssemblies
    |> NUnit3 (fun p ->
        { p with
            ToolPath = "packages/test/NUnit.ConsoleRunner/tools/nunit3-console.exe"
            ShadowCopy = false
            TimeOut = TimeSpan.FromMinutes 20.
//            OutputDir = "bin/tests" 
        })
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "")
                                 .Replace("\n", "")
                                 .Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        ("nuget/" + project + ".nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    Repository.fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

#r "System.Data"
open System.Data
#if MONO 
#r @"packages/test/Mono.Data.Sqlite.Portable/lib/net4/Mono.Data.Sqlite.dll"
open Mono.Data.Sqlite
#else
#r @"packages/test/System.Data.SQLite.Core/lib/net45/System.Data.SQLite.dll"
open System.Data.SQLite
#endif

Target "DbSetup" (fun _ ->
    ensureDirectory "tests/databases"
    for f in !! "tests/sql/*" do
        let dbname = System.IO.Path.GetFileNameWithoutExtension(f)
        use conn = 
        #if MONO 
            new SqliteConnection(sprintf "Data Source=tests/databases/%s.db" dbname)
        #else
            new SQLiteConnection(sprintf "DataSource=tests/databases/%s.db" dbname)
        #endif 
        conn.Open()
        printfn "Creating db: %s" dbname
        let scriptTxt = ReadFileAsString f
        use cmd = conn.CreateCommand()
        cmd.CommandText <- scriptTxt
        use rdr = cmd.ExecuteReader()
        ()
    printfn "Finished!"
)

Target "Release" DoNothing

Target "All" DoNothing

// --------------------------------------------------------------------------------------
// Run 'Build' target by default. Invoke 'build <Target>' to override

"Clean"
    ==> "SetupSqlite"
    ==> "AssemblyInfo" 
    ==> "Build"

"AssemblyInfo"
    ==> "Build"  
    ==> "DbSetup"
    ==> "BuildTest" 
    ==> "RunTests"

"CleanDocs" 
    ==> "GenerateDocs" 
    ==> "ReleaseDocs"

"Build"      
    ==> "GenerateDocs" 
    ==> "All"

"RunTests" 
    ==> "NuGet" 
    ==> "Release"

RunTargetOrDefault "RunTests"
