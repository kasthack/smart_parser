using System.IO;

internal class TestUtil
{
    public static string GetTestDataPath() => Path.GetFullPath(@"..\..\..\testdata\".Replace('\\', Path.DirectorySeparatorChar));
}