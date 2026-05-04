using System.Runtime.CompilerServices;
using VerifyTests;
using VerifyXunit;

namespace FluentReport.Tests;

public static class SnapshotModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        // Place all verified snapshots in tests/FluentReport.Tests/Snapshots/
        Verifier.UseProjectRelativeDirectory("Snapshots");
    }
}
