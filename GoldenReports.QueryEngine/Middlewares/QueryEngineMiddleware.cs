using System.Net.Mime;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Transport;
using GraphQL.Types;

namespace GoldenReports.QueryEngine.Middlewares;

public class QueryEngineMiddleware : IMiddleware
{
    // private readonly GraphQLSettings _settings;
    private readonly IDocumentExecuter executor;
    private readonly IGraphQLSerializer serializer;
    // private readonly ISchema schema;

    public QueryEngineMiddleware(
        // IOptions<GraphQLSettings> options,
        // ISchema schema,
        IDocumentExecuter executor,
        IGraphQLSerializer serializer)
    {
        // _settings = options.Value;
        this.executor = executor;
        this.serializer = serializer;
        // this.schema = schema;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!this.IsGraphQLRequest(context))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        await this.ExecuteAsync(context).ConfigureAwait(false);
    }

    private bool IsGraphQLRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/graphql")//_settings.GraphQLPath
               && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExecuteAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;

        var request = await this.serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted)
            .ConfigureAwait(false);

        var schema = Schema.For(await File.ReadAllTextAsync("test.graphql"), builder =>
        {
            builder.Types.For("Query").FieldFor("userContext").Resolver = new FuncFieldResolver<object>(_ => new {});
            builder.Types.For("User").FieldFor("id").Resolver = new FuncFieldResolver<object>(x => (x as dynamic).Source.id);
            builder.Types.For("User").FieldFor("firstname").Resolver = new FuncFieldResolver<object>(x => (x as dynamic).Source.firstname);
            builder.Types.For("User").FieldFor("lastname").Resolver = new FuncFieldResolver<object>(x => (x as dynamic).Source.lastname);
        });
        // schema.RegisterVisitor<BindToTableVisitor>();

        var result = await this.executor.ExecuteAsync(options =>
        {
            options.Schema = schema;//_schema;
            options.Query = request.Query;
            options.OperationName = request.OperationName;
            options.Variables = request.Variables;
            // options.UserContext = _settings.BuildUserContext?.Invoke(context);
            // options.EnableMetrics = _settings.EnableMetrics;
            options.RequestServices = context.RequestServices;
            options.CancellationToken = context.RequestAborted;
        }).ConfigureAwait(false);

        // if (_settings.EnableMetrics)
        // {
        //     result.EnrichWithApolloTracing(start);
        // }

        await this.WriteResponseAsync(context, result, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task WriteResponseAsync(HttpContext context, ExecutionResult result,
        CancellationToken cancellationToken)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = StatusCodes.Status200OK;
        await this.serializer.WriteAsync(context.Response.Body, result, cancellationToken).ConfigureAwait(false);
    }
}