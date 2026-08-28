using Amazon.CDK;

namespace Chronolog.Server.Infrastructure;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
            throw new ArgumentException("Pass the published Lambda directory as the only argument.");

        var app = new App();
        new ChronologServerStack(app, "ChronologServerStack", args[0]);
        app.Synth();
    }
}