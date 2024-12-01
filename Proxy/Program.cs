using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Custom path transformation to remove trailing slash
        builderContext.AddPathPrefix("");
        builderContext.AddRequestTransform(async transformContext =>
        {
            if (!string.IsNullOrEmpty(transformContext.Path.Value) && transformContext.Path.Value.EndsWith('/'))
            {
                // Remove the trailing slash
                transformContext.Path = transformContext.Path.Value.TrimEnd('/');
            }

            await ValueTask.CompletedTask;
        });

        // Custom transform to clear query parameters
        builderContext.AddRequestTransform(transformContext =>
        {
            transformContext.Query.Collection.Clear();
            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();
app.UseCors("AllowAllOrigins");
app.MapReverseProxy();
app.Use(async (context, next) =>
{
    if (context.Request.Path.HasValue && context.Request.Path.Value.EndsWith('/'))
    {
        context.Request.Path = context.Request.Path.Value.TrimEnd('/');
    }
    await next();
});
app.Run();