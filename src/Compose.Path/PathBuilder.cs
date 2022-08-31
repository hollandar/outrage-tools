﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compose.Path
{
    public class PathBuilder
    {
        private readonly string path;
        private static readonly FileMode[] createModes = new FileMode[] { FileMode.Create, FileMode.CreateNew, FileMode.OpenOrCreate };

        public PathBuilder(string path)
        {
            this.path = path;
        }

        public string Path => path;

        public bool IsDirectory => Directory.Exists(this.Path);
        public bool IsFile => File.Exists(this.path);

        public bool Exists => IsFile || IsDirectory;

        public string Extension => System.IO.Path.GetExtension(this.Path);

        public string FilenameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(this.Path);
        public string Filename => System.IO.Path.GetFileName(this.Path);

        public void CreateDirectory()
        {
            if (!IsDirectory && !IsFile)
                Directory.CreateDirectory(this.path);
        }

        public void EnsureExists()
        {
            if (!this.IsFile && !this.IsDirectory)
                throw new PathDoesNotExistException($"No file or directory exists with the path {this}.");
        }

        public PathBuilder GetDirectory()
        {
            var directoryName = System.IO.Path.GetDirectoryName(this.Path);
            if (directoryName == null)
                throw new PathParentException($"Getting the directory name for {this.Path} results in no directory.");

            return new PathBuilder(directoryName);
        }

        public PathBuilder GetRelativeTo(PathBuilder path)
        {
            var relativePath = System.IO.Path.GetRelativePath(path.Path, this.Path);
            if (relativePath == null)
                throw new Exception($"{this.Path} can not be made relative to {path.Path}.");

            return new PathBuilder(relativePath);
        }

        public async Task<string> ReadToEndAsync()
        {
            if (IsFile)
            {
                using var fileStream = new FileStream(this, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);

                var content = await reader.ReadToEndAsync();

                return content;
            }
            else
                throw new PathFileException($"{this} is not a file, so could not be read.");
        }

        public string ReadToEnd()
        {
            if (IsFile)
            {
                using var fileStream = new FileStream(this, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);

                var content = reader.ReadToEnd();

                return content;
            }
            else
                throw new PathFileException($"{this} is not a file, so could not be read.");
        }

        public async Task WriteAsync(string content)
        {
            if (!IsDirectory)
            {
                using var fileStream = new FileStream(this, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(fileStream);
                await writer.WriteAsync(content);
            }
            else
                throw new PathFileException($"{this} is a directory, it can not be written to.");
        }

        public void Write(string content)
        {
            if (!IsDirectory)
            {
                using var fileStream = new FileStream(this, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(fileStream);
                writer.Write(content);
            }
            else
                throw new PathFileException($"{this} is a directory, it can not be written to.");
        }

        public Stream OpenFilestream(FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite)
        {
            if (IsFile || (!IsDirectory && createModes.Contains(mode)))
            {
                return new FileStream(this, mode, access);
            }

            throw new PathFileException($"{this} exists and is not a file, so can not be read or written.");
        }

        public IEnumerable<PathBuilder> ListContents(string searchPattern = "*", EnumerationOptions? options = null)
        {
            if (IsDirectory)
            {
                var entries = Directory.EnumerateFileSystemEntries(this, searchPattern, options ?? new EnumerationOptions());
                foreach (var entry in entries)
                    yield return new PathBuilder(entry);
            }
            else
                throw new PathDirectoryException($"{this} is not a directory, it can not be content listed.");
        }

        public IEnumerable<PathBuilder> ListFiles(string searchPattern = "*", EnumerationOptions? options = null)
        {
            if (IsDirectory)
            {
                var entries = Directory.EnumerateFiles(this, searchPattern, options ?? new EnumerationOptions());
                foreach (var entry in entries)
                    yield return new PathBuilder(entry);
            }
            else
                throw new PathDirectoryException($"{this} is not a directory, it can not be content listed.");
        }

        public IEnumerable<PathBuilder> ListDirectories(string searchPattern = "*", EnumerationOptions? options = null)
        {
            if (IsDirectory)
            {
                var entries = Directory.EnumerateDirectories(this, searchPattern, options ?? new EnumerationOptions());
                foreach (var entry in entries)
                    yield return new PathBuilder(entry);
            }
            else
                throw new PathDirectoryException($"{this} is not a directory, it can not be content listed.");
        }

        public void Delete(bool recursive = false)
        {
            if (this.IsDirectory)
            {
                Directory.Delete(this, recursive);
            }
            else if (this.IsFile)
            {
                File.Delete(this);
            }
        }

        public void Copy(PathBuilder to)
        {
            if (IsFile)
            {
                File.Copy(this, to);
            }
            else
                throw new PathFileException($"{this} is not a file.");
        }

        public async Task CopyAsync(PathBuilder to)
        {
            if (IsFile)
            {
                using var fromStream = this.OpenFilestream(FileMode.Open, FileAccess.Read);
                using var toStream = to.OpenFilestream(FileMode.Create, FileAccess.Write);
                await fromStream.CopyToAsync(toStream);
            }
            else
                throw new PathFileException($"{this} is not a file.");
        }


        public static PathBuilder operator /(PathBuilder left, PathBuilder right)
        {
            return new PathBuilder(System.IO.Path.Combine(left.path, right.path));
        }

        public static PathBuilder operator /(PathBuilder left, String right)
        {
            return new PathBuilder(System.IO.Path.Combine(left.path, right));
        }

        public static implicit operator PathBuilder?(string? right) => right == null ? null : new PathBuilder(right);

        public static implicit operator string(PathBuilder right) => right.Path;

        public static PathBuilder Root => new PathBuilder("/");

        public override string ToString()
        {
            return this.path;
        }

        public PathBuilder CombineIfRelative(PathBuilder? rootPath = null)
        {
            if (rootPath == null)
            {
                rootPath = new PathBuilder(Directory.GetCurrentDirectory());
            }

            if (!System.IO.Path.IsPathRooted(this.path))
            {
                return new PathBuilder(System.IO.Path.Combine(rootPath, path));
            }

            return this;
        }

        public void Clear()
        {
            foreach (var file in this.ListContents())
            {
                file.Delete(true);
            }
        }
    }
}