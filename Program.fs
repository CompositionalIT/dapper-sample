open Dapper
open Helpers
open Microsoft.Data.SqlClient
open System

// Domain
type CustomerId = CustomerId of Guid
type EmailAddress = EmailAddress of string
[<CLIMutable>]
type MyRecord =
    { Id : CustomerId
      Email : EmailAddress }

// Query
let query = "SELECT Id, Email FROM admin.Customer"
let conn =
    let connection = new SqlConnection("Data Source=(localdb)\\ProjectsV13;Integrated Security=True;Database=CustomerDatabase;Connect Timeout=59;Encrypt=False;MultipleActiveResultSets=True")
    connection.Open()
    connection

(* 1. Using Dapper with type handlers. Supply a type handler which knows how to "unwrap" and "wrap"
   a type to a primitive for SQL. This is often boilerplate so you might want to use e.g. reflection
   to automatically do this on e.g. app startup. *)
SqlMapper.AddTypeHandler(sqlTypeHandler CustomerId (fun (CustomerId id) -> id))
SqlMapper.AddTypeHandler(sqlTypeHandler EmailAddress (fun (EmailAddress id) -> id))

// Use standard Dapper to execute the query into an array of MyRecord.
let data =
    query
    |> conn.Query<MyRecord>
    |> Seq.toArray

(* 2. Using raw ADO .NET with some helper functions. This provides more control and flexibility as you
   are working on a row-by-row, field-by-field basis. Some helper methods we've written (in Helpers.fs)
   that make it simpler to "get at" raw IDataReader fields as types e.g. Guid, String etc. *)
let dataManual =
    query
    |> conn.ExecuteReader
    |> asSequence
    |> Seq.map(fun r ->
        { Id = r.AsGuid "Id" |> CustomerId
          Email = r.AsString "Email" |> EmailAddress })
    |> Seq.toArray

// Print out both arrays - should be the same.
printfn "%A" data
printfn "%A" dataManual