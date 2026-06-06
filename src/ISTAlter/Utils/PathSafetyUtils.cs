// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAlter.Utils;

public static class PathSafetyUtils
{
    public static bool IsPathWithinDirectory(string baseDirectory, string candidatePath)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        var fullBasePath = Path.GetFullPath(baseDirectory);
        var fullCandidatePath = Path.GetFullPath(candidatePath);
        var relativePath = Path.GetRelativePath(fullBasePath, fullCandidatePath);

        return relativePath.Length == 0 ||
               (!Path.IsPathRooted(relativePath) &&
                !relativePath.Equals("..", StringComparison.Ordinal) &&
                !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
    }

    public static bool TryResolveRelativePath(
        string baseDirectory,
        string relativePath,
        out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        var candidatePath = Path.GetFullPath(Path.Join(baseDirectory, relativePath));
        if (!IsPathWithinDirectory(baseDirectory, candidatePath))
        {
            return false;
        }

        fullPath = candidatePath;
        return true;
    }
}
