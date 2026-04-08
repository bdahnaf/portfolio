var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Register WakaTimeService for dependency injection
builder.Services.AddHttpClient<Portfolio.Services.WakaTimeService>();
// Register SpotifyService for dependency injection
builder.Services.AddHttpClient<Portfolio.Services.SpotifyService>();
// Register SteamService for dependency injection
builder.Services.AddHttpClient<Portfolio.Services.SteamService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
