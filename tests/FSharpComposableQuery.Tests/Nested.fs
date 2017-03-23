namespace FSharpComposableQuery.Tests

open FSharpComposableQuery
open NUnit.Framework
open FSharp.Data.Sql
open FSharp.Linq
open System.Linq

/// <summary>
/// Contains example queries and operations on the Organisation database.
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).
/// <para>These tests require the schema from sql/organisation.sql in a database referred to in app.config </para>
/// </summary>
module Nested =
    let [<Literal>] dbpath = __SOURCE_DIRECTORY__ + @"/../databases/organisation.db"
    let [<Literal>] connectionString = "DataSource=" + dbpath + ";Version=3;foreign keys = true"
    let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"../../packages/test/System.Data.Sqlite.Core/net46"
    type sql = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE
            ,   SQLiteLibrary = Common.SQLiteLibrary.SystemDataSQLite
            ,   ConnectionString = connectionString
            ,   ResolutionPath = resolutionPath
            ,   CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >

    let context = sql.GetDataContext ()
    let db = context.Main

    // TypeProvider type abbreviations.
    type Department = sql.dataContext.``main.DepartmentsEntity``
    type Employee   = sql.dataContext.``main.EmployeesEntity``
    type Contact    = sql.dataContext.``main.ContactsEntity``
    type Task       = sql.dataContext.``main.TasksEntity``

    //Nested type declarations
    type EmployeeTasks = {
        Emp : string
        Tasks : System.Linq.IQueryable<string>
    }

    type DepartmentEmployees = {
        Dpt : string
        Employees : System.Linq.IQueryable<EmployeeTasks>
    }

    type NestedOrg = System.Linq.IQueryable<DepartmentEmployees>

    let [<Literal>] N_DEPARTMENTS = 40
    let [<Literal>] N_EMPLOYEES   = 5000
    let [<Literal>] N_ABSTRACTION = 500

    //db tables
    let departments = db.Departments
    let employees   = db.Employees
    let contacts    = db.Contacts
    let tasks       = db.Tasks

    // random data generator
    let rand = new System.Random()
    let mutable idx = 0

    let genId str =
        idx <- idx + 1
        str + "_" + idx.ToString()

    let randomArray (arr : 'a array) =
        let i = rand.Next(arr.Length)
        arr.[i]

    let randomName() =
        randomArray [| "alan"; "bert"; "charlie"; "david"; "edward"; "alice"; "betty"; "clara"; "dora"; "eve" |]
        |> genId

    let randomTask() =
        randomArray [| "abstract"; "buy"; "call"; "dissemble"; "enthuse" |]
        |> genId

    let randomDepartment() =
        randomArray [| "Sales"; "Research"; "Quality"; "Product" |]
        |> genId

    /// <summary>
    /// Generates n uniquely named employees and distributes them uniformly across the given departments.
    /// </summary>
    let randomEmployees n depts =
        [ 1 .. n ]
        |> List.map (fun _ ->
            employees.Create
                (   rand.Next(10000,60000) (* salary *)
                ,   Emp = randomName()
                ,   Dpt = randomArray depts
            )
        )

    /// <summary>
    /// Generates n uniquely named employees in each of the given departments.
    /// </summary>
    let randomEmployeesInEach n depts =
        depts
        |> Array.toList
        |> List.map (fun d -> randomEmployees n [| d |])
        |> List.concat

    /// <summary>
    /// Generates n uniquely named contacts and distributes them uniformly across the given departments.
    /// </summary>
    let randomContacts n depts =
        [ 1..n ]
        |> List.map (fun _ ->
            let contact = contacts.Create()
            contact.Dpt <- randomArray depts
            contact.Contact <- randomName()
            contact.Client <- rand.Next 2
            contact
        )

    /// <summary>
    /// Generates 0 to 2 (inclusive) unique tasks for each of the given employees.
    /// </summary>
    let randomTasks emps =
        emps
        |> List.map (fun (r : Employee) ->
            List.map (fun _ ->
                tasks.Create(Emp = r.Emp, Tsk = randomTask())) [1 .. rand.Next 3])
        |> List.concat


    /// <summary>
    /// Creates a number of random departments and uniformly distributes the specified number of employees across them,
    /// then updates the database with the new rows.
    /// </summary>
    /// <param name="nDep">The number of departments to generate. </param>
    /// <param name="nEmp">The total number of employees to generate. </param>
    let addRandom nDep nEmp =
        let depts = Array.map (ignore >> randomDepartment) [| 1..nDep |]
        let employees = randomEmployees nEmp depts
        let contacts = randomContacts nEmp depts
        let tasks = randomTasks employees
        context.SubmitUpdates()


    /// <summary>
    /// Creates a number of random departments and in each of them generates the specified number of employees,
    /// then updates the database with the new rows.
    /// </summary>
    /// <param name="nDep">The number of departments to generate. </param>
    /// <param name="nEmp">The number of employees to generate in each department. </param>
    let addRandomForEach nDep nEmp =
        let depts = Array.map (ignore >> randomDepartment) [| 1..nDep |]
        let employees = randomEmployeesInEach nEmp depts
        let contacts = randomContacts nEmp depts
        let tasks = randomTasks employees
        context.SubmitUpdates()


    /// <summary>
    /// Creates a department named 'Abstraction' and generates a specified number of employees in it,
    /// each of whom can perform the task "abstract"
    /// <para/>
    /// </summary>
    /// <param name="nEmp">The number of employees to generate in the 'Abstraction' department. </param>
    let addAbstractionDept nEmp =
        departments.Create(Dpt="Abstraction")|>ignore
        let employees = randomEmployees nEmp [| "Abstraction" |]
        let tasks = randomTasks employees
        employees
        |> List.iter (fun (e : Employee) ->
            db.Tasks.Create(Emp = e.Emp, Tsk = "abstract")|>ignore)
        context.SubmitUpdates()


    [<OneTimeSetUp>]
    let init() =
        printf
            "Nested: Adding %d departments, %d employees and additional %d people in the Abstraction department... "
            N_DEPARTMENTS N_EMPLOYEES N_ABSTRACTION
        addRandom N_DEPARTMENTS N_EMPLOYEES
        addAbstractionDept N_ABSTRACTION
        printfn "done!"

    let any () =
        <@ fun xs p -> query {
                for x in xs do exists (p x)
        } @>

    (* There are a number of ways to write each of the following queries *)

    let forallA ()   = <@ fun xs p -> not ((%any()) xs (fun x -> not(p x))) @>
    let forallB ()   = <@ fun xs p -> query { for x in xs do all(p x) } @>
    let containsA () = <@ fun xs u -> (%any()) xs (fun x -> x = u) @>
    let containsB () = <@ fun xs u -> not ((%forallA()) xs (fun x -> x <> u)) @>
    let containsC () = <@ fun xs u -> query { for x in xs do contains u } @>

    let expertiseNaive =
        <@ fun u -> query {
            for d in db.Departments.AsQueryable() do
                if not (query {
                    for e in db.Employees.AsQueryable() do
                        exists (e.Dpt = d.Dpt && not (query {
                            for t in db.Tasks do
                                exists (e.Emp = t.Emp && t.Tsk = u)
                        }))
                })
                then yield d
        } @>

(*
    Example 8 and 9 demonstrate the benefit of using intermediate nested structures.
    Each of them evaluates to the same query but the way we formulate them is inherently different.

    "List all departments where every employee can perform a given task t"
*)


    [<Test>]
    let test01() =
        <@ query {
            yield! (%expertiseNaive) "abstract"
        } @> |> Utils.Run

    let nestedDb = <@ query {
        for d in db.Departments.AsEnumerable() do yield {
            Dpt = d.Dpt
            Employees = query {
                for e in db.Employees.AsQueryable() do
                    if d.Dpt = e.Dpt then yield {
                        Emp   = e.Emp
                        Tasks = query {
                            for t in db.Tasks do
                                if t.Emp = e.Emp then yield t.Tsk
                        }
                    }
            }
        }
    } @>

    let expertise =
        <@ fun u -> query {
            for d in (%nestedDb) do
                if (%forallA()) (d.Employees) (fun e -> (%containsA()) e.Tasks u) then yield d.Dpt
        } @>

    // This query evaluates, but it lazily constructs the result
    // by stitching SQL queries: one for every department and employee.
    // Thus accessing even parts of the data is done by executing
    // multiple queries instead of, possibly, one.
    let test00() =
        query {
            yield! (%nestedDb)
        } |> ignore


    // Example 9


    [<Test>]
    let test02() =
        <@ query {
            yield! (%expertise) "abstract" }
        @> |> Utils.Run