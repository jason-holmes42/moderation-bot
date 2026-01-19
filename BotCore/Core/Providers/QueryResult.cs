using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModerationBot.Core.Providers;

// QueryResult is a container object for results returning from a provider's API.
public class QueryResult
{
    public bool IsSuccessful {  get; init; }    // Easily determine whether the query succeeded.
    public object? Value { get; init; }             // The result object, if any; string, int, TimeSpan, etc.
    public string? Error { get; init; }         // Error, if any.

    QueryResult(object value)
    {
        IsSuccessful = true;
        Value = value;
    }

    QueryResult(string error)
    {
        IsSuccessful = false;
        Error = error;
    }

    // Simple constructors
    public static QueryResult Success(object value) => new(value);
    public static QueryResult Failure(string error) => new(error);
}
