namespace FSharpComposableQuery.Tests

open FSharp.Data.TypeProviders;
open NUnit.Framework;

module NorthwindTests =
    type Northwind = ODataService<"http://services.odata.org/Northwind/Northwind.svc">
    let db = Northwind.GetDataContext()

    // Some tests to compare 
    let dbQuery =  FSharpComposableQuery.TopLevelValues.query

    [<Test>]
    let queryCustomers () = 
        // A query expression.
        query { 
            for x in db.Customers do yield x 
        } |> ignore

    [<Test>]
    let dbQueryCustomers () = 
        // A query expression.
        dbQuery { 
            for x in db.Customers do yield x 
        }|> ignore

    [<Test>]
    let queryInvoices () = 
        // A query expression.
        query { 
            for x in db.Invoices do yield x 
        }|> ignore

    [<Test>]
    let dbQueryInvoices () = 
        dbQuery { 
            for x in db.Invoices do yield x 
        } |> ignore

