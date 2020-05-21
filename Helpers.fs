module Helpers

open Dapper
open System
open System.Data

let sqlTypeHandler (wrap:'TRaw -> 'TRrich) (unwrap: 'TRrich -> 'TRaw) =
    { new SqlMapper.TypeHandler<_>() with
        override __.SetValue(parameter, value) =
            parameter.Value <- value |> unwrap |> box
        override __.Parse value =
            match value with
            | :? 'TRaw as value -> wrap value
            | _ -> failwithf "Unexpected type: Expected '%O', got '%O'" (typeof<'TRaw>.FullName) (value.GetType().FullName) }

[<AutoOpen>]
module DataReaderExtensions =
    let private catchCastException col f =
        try f ()
        with :? System.InvalidCastException as ex ->
            raise (Exception(sprintf "Cast error for column '%s': %s" col ex.Message, ex))

    type System.Data.IDataReader with
        member r.AsMandatory mapper col =
            try catchCastException col (fun () -> r.GetOrdinal col |> mapper)
            with :? Microsoft.Data.SqlClient.SqlException as ex ->
                exn(sprintf "Null value for column '%s'" col, ex)
                |> raise
        member r.AsOptional mapper col =
            catchCastException col (fun () ->
                let index = r.GetOrdinal col
                if r.IsDBNull index then None else Some(mapper index))
        member r.AsInt = r.AsMandatory r.GetInt32
        member r.AsIntOption = r.AsOptional r.GetInt32
        member r.AsDecimal = r.AsMandatory r.GetDecimal
        member r.AsDecimalOption = r.AsOptional r.GetDecimal
        member r.AsDate = r.AsMandatory r.GetDateTime
        member r.AsDateOption = r.AsOptional r.GetDateTime
        member r.AsBool = r.AsMandatory r.GetBoolean
        member r.AsBoolOption = r.AsOptional r.GetBoolean
        member r.AsFloat = r.AsMandatory r.GetDouble
        member r.AsFloatOption = r.AsOptional r.GetDouble
        member r.AsGuidOption = r.AsOptional r.GetGuid
        member r.AsGuid = r.AsMandatory r.GetGuid
        member r.AsString = r.AsMandatory r.GetString
        member r.AsStringOption = r.AsOptional r.GetString

/// Converts a data reader into a sequence that can be mapped over
let asSequence (r:IDataReader) = seq { while r.Read() do r }
