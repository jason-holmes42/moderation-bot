using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core.Providers;

// QueryResult is a container object for results returning from a provider's API.
public class QueryResult
{
    public bool isSuccessful {  get; init; }    // Easily determine whether the query succeeded.
    public object? value { get; init; }             // The result object, if any; string, int, TimeSpan, etc.
    public string? error { get; init; }         // Error, if any.

    QueryResult(object value)
    {
        isSuccessful = true;
        this.value = value;
    }

    QueryResult(string error)
    {
        isSuccessful = false;
        this.error = error;
    }

    // Simple constructors
    public static QueryResult Success(object value) => new(value);
    public static QueryResult Failure(string error) => new(error);
}
