using LazyAndrew;
using Serilog;

Log.Logger = new LoggerConfiguration()
# if DEBUG
    .MinimumLevel.Debug()
# else
    .MinimumLevel.Info()
# endif
    .WriteTo.Console(outputTemplate: "{Message}{NewLine}{Exception}")
    .CreateLogger();

var andrew = new Andrew();
return await andrew.StartAsync();