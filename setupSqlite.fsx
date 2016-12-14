#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake 

let frameworks = ["net40"; "net45"; "net46";"net451"]
frameworks
|> List.iter (fun fwrk ->
    let x86target = sprintf "packages/test/System.Data.SQLite.Core/lib/%s/x86" fwrk
    let x86source = sprintf "packages/test/System.Data.SQLite.Core/build/%s/x86" fwrk
    let x64target = sprintf "packages/test/System.Data.SQLite.Core/lib/%s/x64" fwrk
    let x64source = sprintf "packages/test/System.Data.SQLite.Core/build/%s/x64" fwrk
    CopyDir x86target x86source (fun _ -> true)
    CopyDir x64target x64source (fun _ -> true)
)