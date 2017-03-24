namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Quotations
open System.Linq
open NUnit.Framework
open FSharp.Data.Sql


/// <summary>
/// Contains example queries and operations on the People database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/people.sql in a database referred to in app.config </para>
/// </summary>
[<TestFixture>]
module People =

    [<Literal>]
    let internal N_COUPLES = 5000

    let [<Literal>] dbpath = __SOURCE_DIRECTORY__ + @"/../databases/people.db"
    let [<Literal>] connectionString = 
        #if MONO 
        "Data Source=" + dbpath + ";Version=3;foreign keys = true"
        #else    
        "DataSource=" + dbpath + ";Version=3;foreign keys = true"
        #endif
    let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"../../packages/test/System.Data.Sqlite.Core/net46"

    type sql = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE
            ,   ConnectionString = connectionString
            ,   ResolutionPath = resolutionPath
            ,   CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >

    type Couple = sql.dataContext.``main.CouplesEntity``

    type Person = sql.dataContext.``main.PeopleEntity``

    // Used in example 1
    type Result = { Name : string; Diff : int }

    let context = sql.GetDataContext()
    let db = context.Main

    // Used in example 6
    type Predicate =
        | Above of int
        | Below of int
        | And of Predicate * Predicate
        | Or of Predicate * Predicate
        | Not of Predicate

    let couples = db.Couples
    let people = db.People

    // Clears all relevant tables in the database. 
//    let dropTables() =
//        use conn = new SQLiteConnection(sprintf "DataSource=%s." dbpath)
//        conn.Open()
//        let sqlcmd cmdtxt = 
//            use cmd = new SQLiteCommand(cmdtxt,conn)
//            cmd.ExecuteNonQuery()|>ignore
//
//        sqlcmd "DROP TABLE [People].[dbo].[Couples]"
//        sqlcmd "DROP TABLE [People].[dbo].[People]"
//        conn.Close()

            
    let rnd = new System.Random()

    let mutable idx = 0

    // Appends a unique tag to the given string. 
    let addTag str =
        idx <- idx + 1
        str + "_" + idx.ToString()

    // Picks a random element from the given array. 
    let pickRandom (arr : _ array) =
        let i = rnd.Next(arr.Length)
        arr.[i]

    let maleNames = [| "alan"; "bert"; "charlie"; "david"; "edward" |]
    let femaleNames = [| "alice"; "betty"; "clara"; "dora"; "eve" |]

    let randomAge() = rnd.Next(18, 80)
    let randomMaleName() = (pickRandom >> addTag) maleNames
    let randomFemaleName() = (pickRandom >> addTag) femaleNames
    let randomCouple() = couples.Create(Him = randomMaleName(), Her = randomFemaleName())
    let randomCouples n = List.map (ignore >> randomCouple) [ 1..n ]

    let randomPersons (c:Couple) = [ 
        people.Create(Name = c.Him, Age = randomAge()) 
        people.Create(Name = c.Her, Age = randomAge()) 
    ]

    let randomPeople = (List.map randomPersons >> List.concat)

    // Generates n random couples (and the corresponding people) records and then adds them to the database. 
    let addRandom n =
        let couples = randomCouples n 
        context.SubmitUpdates()
        let people = randomPeople couples
        context.SubmitUpdates()

    [<OneTimeSetUp>]
    let init() =
        printf "People: Adding %d couples... " N_COUPLES
//            dropTables()
        addRandom N_COUPLES
        printfn "done! (%d people; %d couples)" (people.Count()) (couples.Count())

    [<Test>]
    let differences () =
        <@ query {
        for c in db.Couples do
            for w in db.People.AsQueryable() do
                for m in db.People.AsQueryable() do
                    if c.Her = w.Name && c.Him = m.Name && w.Age > m.Age then
                        yield { Name = w.Name; Diff = w.Age - m.Age }
        } @> |> Utils.Run


    [<Test>]
    let test02() =
        let rangeSimple =
            fun (a : int) (b : int) -> query {
                for u in db.People.AsEnumerable() do
                        if a <= u.Age && u.Age < b then
                            yield u
            }
        <@ query { 
            yield! rangeSimple 30 40 
        } @> |> Utils.Run

    let satisfies =
        <@ fun p -> query {
            for u in db.People.AsEnumerable() do
                if p u.Age then
                    yield u
        } @>


    [<Test>]
    let test03() =
        <@ query { 
            yield! (%satisfies) (fun x -> 20 <= x && x < 30) 
        } @> |> Utils.Run

    [<Test>]
    let test04() =
        <@ query { 
            yield! (%satisfies) (fun x -> x % 2 = 0) 
        } @>     |> Utils.Run 


    let range =
        <@ fun (a : int) (b : int) -> query {
            for u in db.People.AsQueryable() do
                if a <= u.Age && u.Age < b then
                    yield u 
        } @>

    let ageFromName =
        <@ fun s -> query {
            for u in db.People.AsQueryable() do
                if s = u.Name then yield u.Age
        } @>

    let compose : Expr<string -> string -> IQueryable<Person>> =
        <@ fun s t -> query {
            for a in (%ageFromName) s do
                for b in (%ageFromName) t do
                    yield! (%range) a b
        } @>


    [<Test>]
    let test05() =
        <@ query { 
            yield! (%compose) "Eve" "Bob" 
        } @> |> Utils.Run 


    let rec eval (t : Predicate) : Expr<int -> bool> =
        match t with
        | Above n   -> <@ fun x -> x >= n @>
        | Below n   -> <@ fun x -> x < n @>
        | And(t1,t2)-> <@ fun x -> (%eval t1) x && (%eval t2) x @>
        | Or(t1,t2) -> <@ fun x -> (%eval t1) x || (%eval t2) x @>
        | Not(t0)   -> <@ fun x -> not ((%eval t0) x) @>

    [<Test>]
    let test06() =
        <@ query { 
            yield! (%satisfies) (%eval (And(Above 20, Below 30))) 
        } @> |> Utils.Run


    let testYieldFrom' = <@ query { 
        for u in db.People.AsQueryable() do
            if 1 <= 0 then
                yield! (query {yield u}) 
    }@>
        
    let testYieldFrom2' = <@ query { 
        for u in db.People do
            if 1 <= 0 then
                yield!(query {
                    for u in db.People.AsEnumerable() do 
                        where (1 <= 0) 
                        yield u}) 
    }@>

    [<Test>]
    let test07() =
        <@ query { 
            yield! (%satisfies) (%eval (Not(Or(Below 20, Above 30)))) 
        } @> |> Utils.Run

        (* not sure if these tests are supposed to pass...
        TODO recheck this
        [<Test>]
        let test000() = 
            printfn "%s" "testYieldFrom"
            Utils.Run testYieldFrom' 

        [<Test>]
        let test001() = 
            printfn "%s" "testYieldFrom2"
            Utils.Run testYieldFrom2' 
            *)
