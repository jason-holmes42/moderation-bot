using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Core.Providers;

// QueryRequest bundles channel identity information, query type, and any necessary parameters for that query to succeed into a single object for passing to providers.
public class QueryRequest
{
    public ChatEndpoint Endpoint { get; init; }
    public QueryType QueryType { get; init; }

    // Parameters necessary for later requests. string username for FollowAge or SubscriptionStatus, for example.

    public QueryRequest(ChatEndpoint endpoint, QueryType queryType)
    {
        Endpoint = endpoint;
        QueryType = queryType;
    }
}
