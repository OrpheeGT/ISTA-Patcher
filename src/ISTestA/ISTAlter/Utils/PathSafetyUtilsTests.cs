// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAlter.Utils;

public class PathSafetyUtilsTests
{
    [Test]
    public void IsPathWithinDirectory_HandlesDirectoryBoundaries()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ISTATest_PathSafety_" + Guid.NewGuid().ToString("N"));
        var basePath = Path.Combine(tempRoot, "base");
        var childPath = Path.Combine(basePath, "child", "file.dll");
        var siblingPath = Path.Combine(tempRoot, "base2", "file.dll");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(childPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(siblingPath)!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, childPath), Is.True);
                Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, siblingPath), Is.False);
                Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, Path.Combine(basePath, "..", "outside.dll")), Is.False);
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
