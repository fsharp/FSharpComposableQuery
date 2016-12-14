namespace FSharpComposableQuery.Tests

open System.Linq
open NUnit.Framework
open FSharp.Data.Sql

/// <summary>
/// Contains example queries and operations on the People database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/people.sql in a database referred to in app.config </para>
/// </summary>
module TPCH =
    
    let [<Literal>] connectionString = "DataSource=" + __SOURCE_DIRECTORY__ + @"/../databases/tpch.db;" + "Version=3;foreign keys = true"
    let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"../../packages/test/System.Data.Sqlite.Core/net46"
    type sql = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE
            ,   SQLiteLibrary = Common.SQLiteLibrary.SystemDataSQLite
            ,   ConnectionString = connectionString
            ,   ResolutionPath = resolutionPath
            ,   CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >

    type  Customer = sql.dataContext.``main.customerEntity``
    type  Lineitem = sql.dataContext.``main.lineitemEntity``
    type  Nation   = sql.dataContext.``main.nationEntity``
    type  Orders   = sql.dataContext.``main.ordersEntity``
    type  Part     = sql.dataContext.``main.partEntity``
    type  Partsupp = sql.dataContext.``main.partsuppEntity``
    type  Region   = sql.dataContext.``main.regionEntity``
    type  Supplier = sql.dataContext.``main.supplierEntity``

    let context = sql.GetDataContext()
    let db = context.Main

    let customers = db.Customer
    let lineitem  = db.Lineitem
    let nation    = db.Nation
    let orders    = db.Orders
    let part      = db.Part
    let partsupp  = db.Partsupp
    let region    = db.Region
    let supplier  = db.Supplier

        /// helper: emptiness test
    let empty () = <@ fun xs -> not (query {for x in xs do exists (true)}) @>
        /// helper: contains 
    let rec contains xs = 
            match xs with 
              [] -> <@ fun x -> false @>
            | y::ys -> <@ fun x -> x = y || (%contains ys) y @> 

    let q1 delta = 
        let date = (new System.DateTime(1998,12,01)).AddDays(-delta) in query { 
            for l in db.Lineitem do 
                where (l.LShipDate <= date)
                groupBy (l.LReturnFlag, l.LLineStatus) into g
                sortBy (g.Key)
                let sum_qty = g.Sum(fun l -> l.LQuantity) in
                let sum_base_price = g.Sum(fun l -> l.LExtendedPrice) in
                let sum_disc_price = g.Sum(fun l -> (decimal(1) - l.LDiscount) * l.LExtendedPrice) in
                let sum_charge = g.Sum(fun l -> (decimal(1) + l.LTax) * (decimal(1) - l.LDiscount) * l.LExtendedPrice) in
                let avg_qty = g.Average(fun l -> l.LQuantity) in 
                let avg_price = g.Average(fun l -> l.LExtendedPrice) in
                let avg_disc = g.Average(fun l -> l.LDiscount) in 
                select (g.Key,sum_qty,sum_base_price,sum_disc_price,sum_charge,avg_qty,avg_price,avg_disc, g.Count) 
        }

       
    let avgBalance = 
        <@ fun (cs : IQueryable<Customer>) -> query { 
            for c in cs do 
                where (c.CAcctBal > decimal(0)) 
                averageBy (c.CAcctBal)
        }@> 
   
    let sumBalance = 
        <@ fun (g : IGrouping<_,Customer>) -> query { 
            for c in g do
                sumBy c.CAcctBal
        } @>
    let ordersOf = 
        <@ fun (c : Customer) -> query { 
            for o in db.Orders do 
                where (o.OCustKey = c.CCustKey)
                select o 
        }@> 
           
    let potentialCustomers = 
        <@ fun (cs : IQueryable<Customer>) -> query { 
            for c in cs do
                where (c.CAcctBal > (%avgBalance) cs && (%empty()) ((%ordersOf) c))  
                select c
        }@>  
    let countryCodeOf = 
        <@ fun (c : Customer) -> c.CPhone.Substring(0,2) @> 
    
    let livesIn countries = 
        <@ fun (c:Customer) -> (%contains countries) ((%countryCodeOf) c) @>

    let pots countries = <@ (%potentialCustomers) (query { 
        for c in db.Customer do 
            where ((%livesIn countries) c)
            select c}) 
    @>  

                // works!
    let q22 (countries : string list) = <@ query { 
        for p in (%pots countries) do 
            groupBy ((%countryCodeOf) p) into g
            sortBy (g.Key)
            select(g.Key, g.Count(), (%sumBalance) g)
    }@>

        (* Trivial change to fix problem with times *)