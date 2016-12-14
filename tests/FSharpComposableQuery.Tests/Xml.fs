namespace FSharpComposableQuery.Tests

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Quotations
open NUnit.Framework
open System.Linq
open System.Xml.Linq
open FSharpComposableQuery
open System.Data.SQLite
open FSharp.Data.Sql

/// <summary>
/// Contains example queries and operations on the Xml database. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para> These tests require the schema from sql/xml.sql in a database referred to in app.config </para>
/// </summary>
module Xml =
    [<Literal>]
    let xmlPath = "data\\movies.xml"
    [<Literal>]
    let dbConfigPath = "db.config"

    let basicXml = XElement.Parse "<a id='1'><b><c>foo</c></b><d><e/><f/></d></a>"

    let [<Literal>] dbpath = __SOURCE_DIRECTORY__ + @"/../databases/xml.db"
    let [<Literal>] connectionString = "DataSource=" + dbpath + ";Version=3;foreign keys = true"
    let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"../../packages/test/System.Data.Sqlite.Core/net46"
    type sql = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE
            ,   SQLiteLibrary = Common.SQLiteLibrary.SystemDataSQLite
            ,   ConnectionString = connectionString
            ,   ResolutionPath = resolutionPath
            ,   CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >
    type internal DataEntity = sql.dataContext.``main.DataEntity``

    type internal Text = sql.dataContext.``main.DataEntity``

    type internal Attribute =  sql.dataContext.``main.AttributeEntity``

    type Axis =
        | Self
        | Child
        | Descendant
        | DescendantOrSelf
        | Following
        | FollowingSibling
        | Rev of Axis
        
    type Path =
        | Seq of Path * Path
        | Axis of Axis
        | Name of string
        | Filter of Path

        static member Child = Axis Child
        static member Self = Axis Self
        static member Descendant = Axis Descendant
        static member DescendantOrSelf = Axis DescendantOrSelf
        static member Parent = Axis(Rev Child)
        static member Ancestor = Axis(Rev Descendant)
        static member AncestorOrSelf = Axis(Rev DescendantOrSelf)
        static member Following = Axis Following
        static member FollowingSibling = Axis FollowingSibling
        static member Preceding = Axis(Rev Following)
        static member PrecedingSibling = Axis(Rev FollowingSibling)

        /// <summary>
        /// Indicates the concatenation of two paths. 
        /// </summary>
        /// <param name="p1">The first path. </param>
        /// <param name="p2">The second path. </param>
        static member (/) (p1, p2) = Seq(p1, p2)

        /// <summary>
        /// Indicates the concatenation of a path with an element name filter. 
        /// </summary>
        /// <param name="path">The initial path. </param>
        /// <param name="name">The name filter for the element. </param>
        static member (%) (path, name) = Seq(path, Name name)

        /// <summary>
        /// Indicates the concatenation of a path with a path filter. 
        /// </summary>
        /// <param name="path1">The initial path. </param>
        /// <param name="path2">The path filter. </param>
        static member (^^) (path1, path2) = Seq(path1, Filter(path2))

    let context = sql.GetDataContext()
    let db = context.Main
    let data = db.Data
    let text = db.Text
    let attributes = db.Attribute

    // XML document loading/shredding

    let mutable idx = 0
    let new_id() =
        let i = idx
        idx <- i + 1
        i

    // Walks the XML document
    let rec traverseXml entry parent i (node : XNode) =
        // creates an attribute record
        let traverseAttribute entry parent (att : XAttribute) =
            let a = db.Attribute.Create()
            a.Element <- parent
            a.Name <- att.Name.LocalName
            a.Value <- att.Value

        // recursively traverse all child nodes
        let traverseChildren entry parent i (xmls) = 
            Seq.fold (traverseXml entry parent) i xmls
        let result =
            match node with
            | :? XElement as xml ->
                let id = new_id()
                let j = Seq.iter (traverseAttribute entry id) (xml.Attributes())
                let j = traverseChildren entry id (i + 1) (xml.Nodes())
                let d = db.Data.Create()
                d.Name <- xml.Name.ToString()
                d.Id <- id
                d.Entry <- entry
                d.Pre <- i
                d.Post <- j
                d.Parent <- parent
                j + 1
            | :? XText as xtext ->
                let id = new_id()
                let d = db.Data.Create()
                d.Name <- "#text"
                d.Id <- id
                d.Entry <- entry
                d.Pre <- i
                d.Post <- i
                d.Parent <- parent
                let t = db.Text.Create()
                t.Id <- id
                t.Value <- xtext.Value
                i + 1
            | _ -> i
        context.SubmitUpdates()
        result

    /// <summary>
    /// Parses the given file as an Xml document, and then inserts its contents in the database. 
    /// </summary>
    /// <param name="filename">The path to the Xml file to be loaded. </param>
    let insertXml entry xml =
        let root_id = new_id()
        let j = traverseXml entry root_id 1 xml
        let d = db.Data.Create()
        d.Id <- root_id
        d.Entry <- entry
        d.Pre <- 0
        d.Post <- j
        d.Parent <- -1
        d.Name <- "#document"
        context.SubmitUpdates()

//        data.InsertOnSubmit(d)
//        data.Context.SubmitChanges()

    /// <summary>
    /// Parses the given file as an Xml document, and then inserts its contents in the database. 
    /// </summary>
    /// <param name="filename">The path to the Xml file to be loaded. </param>
    let loadXml entry (filename : string) =
        let xml = XElement.Load(filename)
        insertXml entry xml

    /// <summary>
    /// Loads the basicXml file
    /// </summary>
    let loadBasicXml() = insertXml 0 basicXml

    /// <summary>
    /// Returns an expression testing whether two rows match the specified axis predicate. 
    /// </summary>
    /// <param name="axis">The axis predicate to test the rows against. </param>
    let internal axisPred axis =
        let rec axisPredRec axis =
            match axis with
            | Self -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Id = row2.Id @>
            | Child -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Id = row2.Parent @>
            | Descendant -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Pre < row2.Pre && row2.Post < row1.Post @>
            | DescendantOrSelf -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Pre <= row2.Pre && row2.Post <= row1.Post @>
            | Following -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Post < row2.Pre @>
            | FollowingSibling -> <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Post < row2.Pre && row1.Parent = row2.Parent @>
            | Rev axis -> <@ fun row1 row2 -> (%axisPredRec axis) row2 row1 @>
        <@ fun (row1 : DataEntity) (row2 : DataEntity) -> row1.Entry = row2.Entry && (%(axisPredRec axis)) row1 row2 @>

    /// <summary>
    /// Returns an expression that will execute the given path query on the specified data source, when provided with a root node. 
    /// </summary>
    /// <param name="data">The data source. </param>
    /// <param name="path">The path to run on the data source. </param>
    let internal pathQuery data (path) =
        let rec pathQ path =
            match path with
            | Seq(p1, p2) ->
                <@ fun row1 row3 ->
                    query {
                        for row2 in %data do
                            exists ((%(pathQ p1)) row1 row2 && (%(pathQ p2)) row2 row3)
                    } @>
            | Axis ax -> <@ fun (row : DataEntity) (row' : DataEntity) -> ((%(axisPred ax)) row row') @>
            | Name name -> <@ fun (row : DataEntity) (row' : DataEntity) -> row.Name = name && row.Id = row'.Id @>
            | Filter p ->
                <@ fun (row : DataEntity) (row' : DataEntity) ->
                    row.Id = row'.Id && query {
                                            for row'' in %data do
                                                exists ((%(pathQ p)) row row'')
                                        } @>
        <@ fun row1 ->
            query {
                for row2 in %data do
                    if (%pathQ path) row1 row2 then yield row2
            } @>

    
    /// <summary>
    /// Translates a path p to a query that returns each node matching p, starting from the root. 
    /// </summary>
    /// <param name="rootId">The id of the root node. </param>
    /// <param name="data">The XML document to run the query on. </param>
    /// <param name="p">The path to construct the query from. </param>
    let internal xpath rootId data p =
        <@ query {
               for root in %data do
                   for row' in (%(pathQuery data p)) root do
                       if (root.Parent = -1 && root.Entry = rootId) then yield row'.Id
           } @>

    let xp0 = Path.Child / Path.Child                                                       //  /*/*
    let xp1 = Path.Descendant / Path.Parent                                                 //  //*/parent::*
    let xp2 = Path.Descendant / (Filter(Path.FollowingSibling % "dirn"))                    //  //*[following-sibling::d]
    let xp3 = Path.Descendant % "year" / Filter(Path.Ancestor / Path.Preceding % "dir")     //  //f[ancestor::*/preceding::b]


    [<OneTimeSetUp>]
    let init() =
        printf "Xml: Parsing file %A... " xmlPath
        loadXml 0 xmlPath
        printfn "done!"

    [<Test>]
    let test01() =
        xp0 |> xpath 0 <@ data @>  |> Utils.Run

    [<Test>]
    let test02() =
        xp1 |> xpath 0 <@ data @> |> Utils.Run

    [<Test>]
    let test03() =
        xp2 |> xpath 0 <@ data @> |> Utils.Run

    [<Test>]
    let test04() =
        xp3 |> xpath 0 <@ data @> |> Utils.Run