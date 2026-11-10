using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

// Content 폴더 경로 설정
var contentPath = Path.Combine(
    app.Environment.ContentRootPath,
    "Content"
);

// Content 폴더의 파일을 HTTPS로 제공
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentPath),
    RequestPath = "/Content"
});

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "GameContentServer is Running!");

app.Run();