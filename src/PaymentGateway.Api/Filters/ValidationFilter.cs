namespace PaymentGateway.Api.Filters
{
    public sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();

            if (argument is null)
            {
                return Results.BadRequest(new
                {
                    error = "Invalid request body",
                    detail = "Request body is required"
                });
            }

            return await next(context);
        }
    }
}
