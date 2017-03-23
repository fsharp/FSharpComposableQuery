namespace FSharpComposableQuery.Tests

open System
open System.Linq
open System.Data.Linq.SqlClient
open NUnit.Framework
open FSharp.Data.Sql

open FSharpComposableQuery

/// <summary>
/// Contains the modified queries from Microsoft's F# example expressions page. 
/// The queries here are further wrapped in quotations to allow for their evaluation in different contexts (see Utils.fs).  
/// <para>These tests require the schema from sql/simple.sql in a database referred to in app.config </para>
/// <para>The original queries can be found at http://msdn.microsoft.com/en-us/library/vstudio/hh225374.aspx </para>
/// </summary>
module Simple = 

    let [<Literal>] connectionString = "DataSource=" + __SOURCE_DIRECTORY__ + @"/../databases/simple.db;" + "Version=3;foreign keys = true"
    let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"../../packages/test/System.Data.Sqlite.Core/net46"
    type sql = SqlDataProvider<
                Common.DatabaseProviderTypes.SQLITE
            ,   ConnectionString = connectionString
            ,   ResolutionPath = resolutionPath
            ,   CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
            >

    let context = sql.GetDataContext()
    
    let db = sql.GetDataContext().Main
        
    let data = [1; 5; 7; 11; 18; 21]

    let mutable idx = 0
    // Generates a unique tag for each consecutive query
    let tag s = 
        idx <- idx + 1
        printfn "Q%02d: %s" idx s
        
    [<Test>]
    let ``contains query operator``() = 
        tag "contains query operator"
        <@ query {
            for student in db.Student do
            select student.Age
            contains 11
        } @> |> Utils.Run


    [<Test>]
    let ``count query operator``() = 
        tag "count query operator"
        <@ query {
            for student in db.Student do
            select student
            count
        } @> |> Utils.Run



    [<Test>]
    let ``last query operator.``() = 
        tag "last query operator." 
        <@ query {
            for s in data do
            sortBy s
            last
        } @> |> Utils.Run


    [<Test>]
    let ``lastOrDefault query operator.``() = 
        tag "lastOrDefault query operator." 
        <@ query {
            for number in data do
            sortBy number
            lastOrDefault
        } @> |> Utils.Run


    [<Test>]
    let ``exactlyOne query operator.``() = 
        tag "exactlyOne query operator."
        <@ query {
            for student in db.Student do
            where (student.StudentId = 1)
            select student
            exactlyOne
        } @> |> Utils.Run


    [<Test>]
    let ``exactlyOneOrDefault query operator.``() = 
        tag "exactlyOneOrDefault query operator."
        <@ query {
            for student in db.Student do
            where (student.StudentId = 1)
            select student
            exactlyOneOrDefault
        } @> |> Utils.Run


    [<Test>]
    let ``headOrDefault query operator.``() = 
        tag "headOrDefault query operator."
        <@ query {
            for student in db.Student do
            select student
            headOrDefault
        } @> |> Utils.Run


    [<Test>]
    let ``select query operator.``() = 
        tag "select query operator."
        <@ query {
                for (student) in db.Student do
                select student
        } @> |> Utils.Run



    [<Test>]
    let ``where query operator.``() = 
        tag "where query operator."
        <@ query {
                for student in db.Student do
                where (student.StudentId > 4)
                select student
        } @> |> Utils.Run


    [<Test>]
    let ``minBy query operator.``() = 
        tag "minBy query operator."
        <@ query {
            for student in db.Student do
            minBy student.StudentId
        } @> |> Utils.Run



    [<Test>]
    let ``maxBy query operator.``() = 
        tag "maxBy query operator."
        <@ query {
            for student in db.Student do
            maxBy student.StudentId
        } @> |> Utils.Run


    

    [<Test>]
    let ``groupBy query operator.``() = 
        tag "groupBy query operator."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupBy student.Age into g
            select (g.Key, g.Count())
        } @> |> Utils.Run



    [<Test>]
    let ``sortBy query operator.``() = 
        tag "sortBy query operator."
        <@ query {
            for student in db.Student do
            sortBy student.Name
            select student
        } @> |> Utils.Run



    [<Test>]
    let ``sortByDescending query operator.``() = 
        tag "sortByDescending query operator."
        <@ query {
            for student in db.Student do
            sortByDescending student.Name
            select student
        } @>
        |> Utils.Run



    [<Test>]
    let ``thenBy query operator.``() = 
        tag "thenBy query operator."
        <@ query {
            for student in db.Student do
            where (student.Age <> -1)
            sortBy student.Age
            thenBy student.Name
            select student
        } @>
        |> Utils.Run



    [<Test>]
    let ``thenByDescending query operator.``() = 
        tag "thenByDescending query operator."
        <@ query {
            for student in db.Student do
            where (student.Age <> -1)
            sortBy student.Age
            thenByDescending student.Name
            select student
        } @>
        |> Utils.Run



    [<Test>]
    let ``groupValBy query operator.``() = 
        tag "groupValBy query operator."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupValBy student.Name student.Age into g
            select (g, g.Key, g.Count())
            } @>
        |> Utils.Run



    [<Test>]
    let ``sumByNullable query operator``() = 
        tag "sumByNullable query operator"
        <@ query {
            for student in db.Student do
            sumByNullable (Nullable student.Age)
            } @>
        |> Utils.Run



    [<Test>]
    let ``minByNullable``() = 
        tag "minByNullable"
        <@ query {
            for student in db.Student do
            minByNullable (Nullable student.Age)
            } @>
        |> Utils.Run



    [<Test>]
    let ``maxByNullable``() = 
        tag "maxByNullable"
        <@ query {
            for student in db.Student do
            maxByNullable (Nullable student.Age)
            } @>
        |> Utils.Run



    [<Test>]
    let ``averageBy``() = 
        tag "averageBy"
        <@ query {
            for student in db.Student.AsEnumerable() do
            averageBy (float student.StudentId)
            } @>
        |> Utils.Run



    [<Test>]
    let ``averageByNullable``() = 
        tag "averageByNullable"
        <@ query {
            for student in db.Student.AsEnumerable() do
                averageByNullable (Nullable <| float student.Age)
            } @>
        |> Utils.Run



    [<Test>]
    let ``find query operator``() = 
        tag "find query operator"
        <@ query {
            for student in db.Student do
                find (student.Name = "Abercrombie, Kim")
        } @>
        |> Utils.Run



    [<Test>]
    let ``all query operator``() = 
        tag "all query operator"
        <@ query {
            for student in db.Student do
                all (SqlMethods.Like(student.Name, "%,%"))
        } @>
        |> Utils.Run



    [<Test>]
    let ``head query operator``() = 
        tag "head query operator"
        <@ query {
            for student in db.Student do
            head
            } @>
        |> Utils.Run



    [<Test>]
    let ``nth query operator``() = 
        tag "nth query operator"
        <@ query {
            for numbers in data do
            nth 3
            } @>
        |> Utils.Run



    [<Test>]
    let ``skip query operator``() = 
        tag "skip query operator"
        <@ query {
            for student in db.Student do
            skip 1
            } @>
        |> Utils.Run



    [<Test>]
    let ``skipWhile query operator``() = 
        tag "skipWhile query operator"
        <@ query {
            for number in data do
            skipWhile (number < 3)
            select number
            } @>
        |> Utils.Run



    [<Test>]
    let ``sumBy query operator``() = 
        tag "sumBy query operator"
        <@ query {
            for student in db.Student do
            sumBy student.StudentId
        } @> |> Utils.Run


    [<Test>]
    let ``take query operator``() = 
        tag "take query operator"
        <@ query {
            for student in db.Student do
            select student
            take 2
        } @> |> Utils.Run


    [<Test>]
    let ``takeWhile query operator``() = 
        tag "takeWhile query operator"
        <@ query {
            for number in data do
            takeWhile (number < 10)
        } @> |> Utils.Run


    [<Test>]
    let ``sortByNullable query operator``() = 
        tag "sortByNullable query operator"
        <@ query {
            for student in db.Student.AsEnumerable() do
            sortByNullable (Nullable student.Age)
            select student
        } @> |> Utils.Run


    [<Test>]
    let ``sortByNullableDescending query operator``() = 
        tag "sortByNullableDescending query operator"
        <@ query {
            for student in db.Student.AsEnumerable() do
            sortByNullableDescending (Nullable student.Age)
            select student
        } @> |> Utils.Run


    [<Test>]
    let ``thenByNullable query operator``() = 
        tag "thenByNullable query operator"
        <@ query {
            for student in db.Student.AsEnumerable() do
            sortBy student.Name
            thenByNullable (Nullable student.Age)
            select student
        } @> |> Utils.Run


    [<Test>]
    let ``thenByNullableDescending query operator``() = 
        tag "thenByNullableDescending query operator"
        <@ query {
            for student in db.Student.AsEnumerable() do
            sortBy student.Name
            thenByNullableDescending (Nullable student.Age)
            select student
        } @> |> Utils.Run


    [<Test>]
    let ``All students:``() = 
        tag "All students: "
        <@ query {
            for student in db.Student do select student
        } @> |> Utils.Run


    [<Test>]
    let ``Count of students:``() = 
        tag "Count of students: "
        <@ query {
            for student in db.Student.AsEnumerable() do count
        } @> |> Utils.Run


    [<Test>]
    let ``Exists.``() = 
        tag "Exists, native QueryBuilder."
        <@ query {
            for student in db.Student.AsEnumerable() do
                where (ExtraTopLevelOperators.query 
                    { for courseSelection in db.CourseSelection do
                        exists (courseSelection.StudentId = student.StudentId) })
                select student 
        } @> |> Utils.Run

    

    [<Test>]
    let ``Exists (bug).``() = 
        tag "Exists."
        <@ query {
            for student in db.Student.AsEnumerable() do
                where (query 
                    { for courseSelection in db.CourseSelection do
                        exists (courseSelection.StudentId = student.StudentId) })
                select student } @>
        |> Utils.Run


    [<Test>]
    let ``Group by age and count``() = 
        tag "Group by age and count"
        <@ query {
            for n in db.Student.AsEnumerable() do
            groupBy n.Age into g
            select (g.Key, g.Count())
        } @> |> Utils.Run


    [<Test>]
    let ``Group value by age.``() = 
        tag "Group value by age."
        <@ query {
            for n in db.Student.AsEnumerable() do
            groupValBy n.Age n.Age into g
            select (g.Key, g.Count())
        } @> |> Utils.Run


    [<Test>]
    let ``Group students by age where age > 10.``() = 
        tag "Group students by age where age > 10."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupBy student.Age into g
            where (g.Key > 10)
            select (g, g.Key)
        } @> |> Utils.Run


    [<Test>]
    let ``Group students by age and print counts of number of students at each age with more than 1 student.``() = 
        tag "Group students by age and print counts of number of students at each age with more than 1 student."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupBy student.Age into group
            where (group.Count() > 1)
            select (group.Key, group.Count())
        } @> |> Utils.Run


    [<Test>]
    let ``Group students by age and sum ages.``() = 
        tag "Group students by age and sum ages."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupBy student.Age into g        
            let total = query { for student in g do sumByNullable (Nullable student.Age) }
            select (g.Key, g.Count(), total)
        } @> |> Utils.Run


    [<Test>]
    let ``Group students by age and count number of students at each age, and display all with count > 1 in descending order of count.``() = 
        tag "Group students by age and count number of students at each age, and display all with count > 1 in descending order of count."
        <@ query {
            for student in db.Student.AsEnumerable() do
            groupBy student.Age into g
            where (g.Count() > 1)        
            sortByDescending (g.Count())
            select (g.Key, g.Count())
        } @> |> Utils.Run


    [<Test>]
    let ``Select students from a set of IDs``() = 
        tag "Select students from a set of IDs"
        let idList = [1; 2; 5; 10]
        let idQuery = query { for id in idList do select id }
        <@ query {
            for student in db.Student do
            where (idQuery.Contains(student.StudentId))
            select student
        } @> |> Utils.Run


    [<Test>]
    let ``Look for students with Name match _e%% pattern and take first two.``() = 
        tag "Look for students with Name match _e%% pattern and take first two."
        <@ query {
            for student in db.Student do
            where (SqlMethods.Like( student.Name, "_e%") )
            select student
            take 2   
        } @> |> Utils.Run


    [<Test>]
    let ``Look for students with Name matching [abc]%% pattern.``() = 
        tag "Look for students with Name matching [abc]%% pattern."
        <@ query {
            for student in db.Student do
            where (SqlMethods.Like( student.Name, "[abc]%") )
            select student  
        } @> |> Utils.Run


    [<Test>]
    let ``Look for students with name matching [^abc]%% pattern.``() = 
        tag "Look for students with name matching [^abc]%% pattern."
        <@ query {
            for student in db.Student do
            where (SqlMethods.Like( student.Name, "[^abc]%") )
            select student  
        } @> |> Utils.Run 


    [<Test>]
    let ``Look for students with name matching [^abc]%% pattern and select ID.``() = 
        tag "Look for students with name matching [^abc]%% pattern and select ID."
        <@ query {
            for n in db.Student do
            where (SqlMethods.Like( n.Name, "[^abc]%") )
            select n.StudentId    
        } @> |> Utils.Run


    [<Test>]
    let ``Using Contains as a query filter.``() = 
        tag "Using Contains as a query filter."
        <@ query {
            for student in db.Student do
            where (student.Name.Contains("a"))
            select student
        } @> |> Utils.Run



    [<Test>]
    let ``Searching for names from a list.``() = 
        tag "Searching for names from a list."
        let names = [|"a";"b";"c"|]
        <@ query {
            for student in db.Student do
            if names.Contains (student.Name) then select student } 
        @> |> Utils.Run


    [<Test>]
    let ``Join Student and CourseSelection tables.``() = 
        tag "Join Student and CourseSelection tables."
        <@ query {
                for student in db.Student.AsEnumerable() do 
                join selection in db.CourseSelection 
                    on (student.StudentId = selection.StudentId)
                select (student, selection)
            } @>
        |> Utils.Run



    [<Test>]
    let ``Left Join Student and CourseSelection tables.``() = 
        tag "Left Join Student and CourseSelection tables."
        <@ query {
            for student in db.Student.AsEnumerable() do
            leftOuterJoin selection in db.CourseSelection 
                on (student.StudentId = selection.StudentId) into result
            for selection in result.DefaultIfEmpty() do
            select (student, selection)
            } @>
        |> Utils.Run



    [<Test>]
    let ``Join with count``() = 
        tag "Join with count"
        <@ query {
                for n in db.Student.AsEnumerable() do 
                join e in db.CourseSelection on (n.StudentId = e.StudentId)
                count        
            } @>
        |> Utils.Run



    [<Test>]
    let ``Join with distinct.``() = 
        tag "Join with distinct."
        <@ query {
                for student in db.Student.AsEnumerable() do 
                join selection in db.CourseSelection on (student.StudentId = selection.StudentId)
                distinct        
            } @>
        |> Utils.Run



    [<Test>]
    let ``Join with distinct and count.``() = 
        tag "Join with distinct and count."
        <@ query {
                for n in db.Student.AsEnumerable() do 
                join e in db.CourseSelection on (n.StudentId = e.StudentId)
                distinct
                count       
            } @>
        |> Utils.Run



    [<Test>]
    let ``Selecting students with age between 10 and 15.``() = 
        tag "Selecting students with age between 10 and 15."
        <@ query {
                for student in db.Student do
                where (student.Age >= 10 && student.Age < 15)
                select student
            } @>
        |> Utils.Run



    [<Test>]
    let ``Selecting students with age either 11 or 12.``() = 
        tag "Selecting students with age either 11 or 12."
        <@ query {
                for student in db.Student do
                where (student.Age = 11 || student.Age = 12)
                select student
            } @>
        |> Utils.Run



    [<Test>]
    let ``Selecting students in a certain age range and sorting.``() = 
        tag "Selecting students in a certain age range and sorting."
        <@ query {
                for n in db.Student do
                where (n.Age = 12 || n.Age = 13)
                sortByDescending  n.Age
                select n
            } @>
        |> Utils.Run



    [<Test>]
    let ``Selecting students with certain ages, taking account of possibility of nulls.``() = 
        tag "Selecting students with certain ages, taking account of possibility of nulls."
        <@ query {
                for student in db.Student do
                where ((student.Age = 11) ||
                        (student.Age = 12))
                sortByDescending student.Name 
                select student.Name
                take 2
            } @>
        |> Utils.Run

            
            
    [<Test>]
    let ``Union of two queries.``() = 
        tag "Union of two queries."

        let query1 = <@ query {
                for n in db.Student do
                select (n.Name, n.Age)
            } @>

        let query2 = <@ query {
                for n in db.LastStudent do
                select (n.Name, n.Age)
                } @>

        <@ query { for n in (%query1).Union(%query2) do select n } @>
        |> Utils.Run

    [<Test>]
    let ``Union of two queries (enumerable)``() = 
        tag "Union of two queries (enumerable)"

        let la = [1;2;3;4]
        let query1 = <@ query { for n in la do yield n } @>

        let lb = [3;4;5;6]
        let query2 = <@ query { for n in lb do yield n } @>

        <@ query { yield! (%query1).Union(%query2) } @>
        |> Utils.Run



    [<Test>]
    let ``Intersect of two queries.``() = 
        tag "Intersect of two queries."

        let query1 = <@ query { for n in db.Student do select (n.Name, n.Age) } @>
        let query2 = <@ query { for n in db.LastStudent do select (n.Name, n.Age) } @>

        <@ query { yield! (%query1).Intersect(%query2) } @>

        |> Utils.Run


    [<Test>]
    let ``Using if statement to alter results for special value.``() = 
        tag "Using if statement to alter results for special value."
        <@ query {
                for student in db.Student do
                select (if student.Age = -1 then
                            (student.StudentId, 100, student.Age)
                        else (student.StudentId, student.Age, student.Age))
            } @>
        |> Utils.Run



    [<Test>]
    let ``Using if statement to alter results special values.``() = 
        tag "Using if statement to alter results special values."
        <@ query {
                for student in db.Student do
                select (if  student.Age = -1 then
                            (student.StudentId, 100, student.Age)
                        elif student.Age = 0 then
                            (student.StudentId, 100, student.Age)
                        else (student.StudentId, student.Age, student.Age))
            } @>
        |> Utils.Run




    [<Test>]
    let ``Multiple table select.``() = 
        tag "Multiple table select."
        <@ query {
                for student in db.Student.AsEnumerable() do
                for course in db.Course.AsEnumerable() do
                select (student, course)
        } @>
        |> Utils.Run



    [<Test>]
    let ``Multiple Joins``() = 
        tag "Multiple Joins"
        <@ query {
            for student in db.Student.AsEnumerable() do
            join courseSelection in db.CourseSelection on
                (student.StudentId = courseSelection.StudentId)
            join course in db.Course on
                    (courseSelection.CourseId = course.CourseId)
            select (student.Name, course.CourseName)
        } @>
        |> Utils.Run



    [<Test>]
    let ``Multiple Left Outer Joins``() = 
        tag "Multiple Left Outer Joins"
        <@ query {
            for student in db.Student.AsEnumerable() do
            leftOuterJoin courseSelection in db.CourseSelection 
                on (student.StudentId = courseSelection.StudentId) into g1
            for courseSelection in g1.DefaultIfEmpty() do
            leftOuterJoin course in db.Course 
                on (courseSelection.CourseId = course.CourseId) into g2
            for course in g2.DefaultIfEmpty() do
            select (student.Name, course.CourseName)
            } @>
        |> Utils.Run

