using System.Text.Json;
using Pulumi;

public class DevStack : Stack
{
    [Output]
    public Output<string> ServiceName { get; set; }

    public DevStack()
    {
        // var config = new Config();
        // var data = config.RequireObject<JsonElement>("data");
        // Log.Info(data.ToString());
    }
}