// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Services;

using System.Reflection;
using global::ISTAlter.Utils;
using global::ISTAPatcher.Commands;

public class CryptoIntegrityPathTests
{
    [Test]
    public async Task CheckFileIntegrity_RejectsEscapingPath()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "ISTATest_CryptoBase_" + Guid.NewGuid().ToString("N"));
        var outsidePath = Path.Combine(Path.GetTempPath(), "ISTATest_CryptoOutside_" + Guid.NewGuid().ToString("N") + ".dll");

        try
        {
            Directory.CreateDirectory(basePath);
            await File.WriteAllTextAsync(outsidePath, "outside");

            var relativePath = Path.GetRelativePath(basePath, outsidePath);
            var fileInfo = new HashFileInfo([relativePath, string.Empty]);
            var result = await InvokeCheckFileIntegrity(basePath, fileInfo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Key, Is.EqualTo("[yellow]404[/]"));
                Assert.That(result.Value, Is.EqualTo("Invalid path"));
            }
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, recursive: true);
            }

            if (File.Exists(outsidePath))
            {
                File.Delete(outsidePath);
            }
        }
    }

    private static async Task<KeyValuePair<string, string>> InvokeCheckFileIntegrity(string basePath, HashFileInfo fileInfo)
    {
        var method = typeof(CryptoCommand).GetMethod("CheckFileIntegrity", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CheckFileIntegrity method not found.");
        var task = (Task<KeyValuePair<string, string>>)method.Invoke(null, [basePath, fileInfo, CancellationToken.None])!;
        return await task;
    }
}
