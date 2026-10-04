using System;
using System.Runtime.InteropServices;

try
{
    // Phase 1 self-check: exercises every DSA module in isolation and prints
    // PASS/FAIL lines to the console before the game window opens.
    TileQuest.DsaDemo.Run();

    using var game = new TileQuest.Game1();
    game.Run();
}
catch (Exception exception)
{
    string details = exception.ToString();
    Console.Error.WriteLine(details);

    if (OperatingSystem.IsWindows())
    {
        MessageBox(
            IntPtr.Zero,
            exception.Message,
            "TileQuest could not start",
            0x00000000 | 0x00000010);
    }
}

[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
static extern int MessageBox(IntPtr window, string text, string caption, uint type);
