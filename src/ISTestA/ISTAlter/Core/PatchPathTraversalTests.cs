// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAlter.Core;

/// <summary>
/// Tests for path traversal vulnerability in Patch.PatchSingleFile.
/// </summary>
public class PatchPathTraversalTests
{
    private string _testBasePath = null!;
    private string _tempTargetPath = null!;

    [SetUp]
    public void Setup()
    {
        // Create temporary directories for testing
        _testBasePath = Path.Combine(Path.GetTempPath(), "ISTATest_Base_" + Guid.NewGuid().ToString("N"));
        _tempTargetPath = Path.Combine(Path.GetTempPath(), "ISTATest_Target_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testBasePath);
        Directory.CreateDirectory(_tempTargetPath);

        // Create a dummy file to patch
        var testFile = Path.Combine(_testBasePath, "test.dll");
        File.WriteAllText(testFile, "dummy content");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up temporary directories
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
        if (Directory.Exists(_tempTargetPath))
        {
            Directory.Delete(_tempTargetPath, recursive: true);
        }
    }

    /// <summary>
    /// Test that path traversal sequences in Include configuration can escape the intended directory.
    /// This demonstrates the vulnerability described in FULL_CODEBASE_REVIEW.md issue #1.
    /// </summary>
    [Test]
    public void PatchSingleFile_PathTraversal_CanEscapeBaseDirectory()
    {
        // Arrange: Create a sensitive file outside the base directory
        var sensitiveFile = Path.Combine(_tempTargetPath, "sensitive.dll");
        File.WriteAllText(sensitiveFile, "sensitive content");

        // Calculate relative path from base to target using traversal
        var relativePath = Path.GetRelativePath(_testBasePath, sensitiveFile);

        // Act: Try to access the file via path traversal
        var traversalPath = Path.Join(_testBasePath, relativePath);
        var normalizedPath = Path.GetFullPath(traversalPath);

        // Assert: The normalized path escapes the base directory
        Assert.That(normalizedPath, Is.EqualTo(Path.GetFullPath(sensitiveFile)),
            "Path traversal should resolve to the sensitive file");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(normalizedPath.StartsWith(Path.GetFullPath(_testBasePath), StringComparison.OrdinalIgnoreCase),
                    Is.False,
                    "Traversal path should escape the base directory");
            Assert.That(File.Exists(normalizedPath), Is.True,
                "The traversal path should resolve to an existing file outside the base directory");
        }
    }

    /// <summary>
    /// Test that Windows-style path traversal works.
    /// </summary>
    [Test]
    public void PatchSingleFile_WindowsPathTraversal_CanEscapeBaseDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Pass("Backslash traversal is only normalized as directory traversal on Windows.");
        }

        // Arrange
        var sensitiveFile = Path.Combine(_tempTargetPath, "important.dll");
        File.WriteAllText(sensitiveFile, "important content");

        // Create a traversal path using Windows-style separators
        var depth = _testBasePath.Split(Path.DirectorySeparatorChar).Length;
        var traversal = string.Join("\\", Enumerable.Repeat("..", depth)) + "\\" +
                       string.Join("\\", _tempTargetPath.Split(Path.DirectorySeparatorChar).Skip(1)) + "\\important.dll";

        // Act
        var traversalPath = Path.Join(_testBasePath, traversal);
        var normalizedPath = Path.GetFullPath(traversalPath);

        // Assert
        Assert.That(File.Exists(normalizedPath), Is.True,
            "Windows-style traversal should resolve to existing file");
        Assert.That(normalizedPath.StartsWith(Path.GetFullPath(_testBasePath), StringComparison.OrdinalIgnoreCase),
            Is.False,
            "Windows-style traversal should escape base directory");
    }

    /// <summary>
    /// Test that production path validation rejects traversal attempts that resolve outside the base directory.
    /// </summary>
    [Test]
    public void ValidatePath_RejectsPathTraversal()
    {
        var basePath = Path.GetFullPath(_testBasePath);
        var traversalAttempt = Path.Combine("..", "sensitive.dll");
        var fullPath = Path.GetFullPath(Path.Join(basePath, traversalAttempt));

        Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, fullPath), Is.False,
            "Path traversal should be detected and rejected by validation");
    }

    /// <summary>
    /// Test that production path validation accepts legitimate paths within the base directory.
    /// </summary>
    [Test]
    public void ValidatePath_AcceptsLegitimateSubdirectoryPath()
    {
        var basePath = Path.GetFullPath(_testBasePath);
        var legitimatePath = Path.Combine("subdir", "file.dll");
        var fullPath = Path.GetFullPath(Path.Join(basePath, legitimatePath));

        Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, fullPath), Is.True,
            "Legitimate subdirectory paths should be accepted");
    }

    /// <summary>
    /// Test that paths in sibling directories with the same textual prefix are rejected.
    /// </summary>
    [Test]
    public void ValidatePath_RejectsSiblingDirectoryWithSamePrefix()
    {
        var parentPath = Path.GetDirectoryName(_testBasePath)!;
        var basePath = Path.Combine(parentPath, "ISTA");
        var siblingPath = Path.Combine(parentPath, "ISTA2", "file.dll");
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.GetDirectoryName(siblingPath)!);

        Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, siblingPath), Is.False,
            "Sibling directories with the same textual prefix must not be accepted");
    }

    /// <summary>
    /// Test that resolving relative paths rejects rooted paths and relative paths that escape the base directory.
    /// </summary>
    [Test]
    public void TryResolveRelativePath_RejectsRootedAndEscapingPaths()
    {
        var basePath = Path.GetFullPath(_testBasePath);
        var rootedPath = Path.GetFullPath(Path.Combine(_tempTargetPath, "outside.dll"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(PathSafetyUtils.TryResolveRelativePath(basePath, Path.Combine("subdir", "file.dll"), out var resolved), Is.True);
            Assert.That(PathSafetyUtils.IsPathWithinDirectory(basePath, resolved), Is.True);
            Assert.That(PathSafetyUtils.TryResolveRelativePath(basePath, Path.Combine("..", "outside.dll"), out _), Is.False);
            Assert.That(PathSafetyUtils.TryResolveRelativePath(basePath, rootedPath, out _), Is.False);
        }
    }
}
