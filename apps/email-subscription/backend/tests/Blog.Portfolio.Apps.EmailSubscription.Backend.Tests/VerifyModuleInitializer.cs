using System.Runtime.CompilerServices;
using DiffEngine;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tests;

internal static class VerifyModuleInitializer
{
    // Agents run this suite unattended and can read console output but cannot see or dismiss a GUI window,
    // so the diff-tool launcher is off and DiffPlex prints the diff inline in the test failure instead.
    [ModuleInitializer]
    public static void Initialize()
    {
        DiffRunner.Disabled = true;
        VerifyDiffPlex.Initialize();
    }
}
