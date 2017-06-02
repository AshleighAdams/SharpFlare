using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharpFlare
{
	public static class Util
	{
		//                       0th   1st   2nd   3rd   4th   5th   6th   7th   8th   9th
		static string[] nths = { "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th" };
		public static string Nth(int n)
		{
			int mod100 = n % 100;

			if(mod100 >= 11 && mod100 <= 13)
				return nths[0];
			return nths[mod100 % 10];
		}

		public static string ToHttpDate(this DateTime when)
		{
			//  Sun, 06 Nov 1994 08:49:37 GMT
			DateTime utc = when.ToUniversalTime();
			return utc.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
		}

		// gets a more traditional and readable stack trace
		static Regex asyncregex = new Regex("at (?<namespace>.*)\\.<(?<method>.*)>(?<bit>.*).MoveNext\\(\\) in (?<file>.*):line (?<line>[0-9]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		static Regex syncregex = new Regex("at (?<method>.*) in (?<file>.*):line (?<line>[0-9]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		[CLI.Option("Show the directory on public stack traces.", "debug-stack-show-directory")]
		public static bool StackShowDir = false;

		public static string SourceCodeBase = "";
		public static string CleanAsyncStackTrace(string stacktrace, bool ispublic = true)
		{
			string[] lines = stacktrace.Split('\n');
			StringBuilder sb = new StringBuilder();
			
			foreach (string _ in lines)
			{
				string line = _.Trim();

				if (line == "--- End of stack trace from previous location where exception was thrown ---")
					continue;
				if (line.Contains("System.Runtime.CompilerServices.TaskAwaiter"))
					continue;

				if (line.Contains(".MoveNext()"))
				{
					Match match = asyncregex.Match(line);
					if (match.Success)
					{
						string @namespace = match.Groups["namespace"].Value;
						string method = match.Groups["method"].Value;
						string file = match.Groups["file"].Value;
						string linenum = match.Groups["line"].Value;

						// sanatize the file
						file = file.Replace('\\', '/').Replace(Util.SourceCodeBase, "SharpFlare");
						line = $"{file}:{linenum} in async {@namespace}.{method}(...)";
						sb.AppendLine(line);
					}
				}
				else
				{
					Match match = syncregex.Match(line);

					if (match.Success)
					{
						string method = match.Groups["method"].Value;
						string file = match.Groups["file"].Value;
						string linenum = match.Groups["line"].Value;

						// sanatize the file
						file = file.Replace('\\', '/').Replace(Util.SourceCodeBase, "SharpFlare");

						line = $"{file}:{linenum} in {method}";
						sb.AppendLine(line);
					}
				}

				
			}

			return sb.ToString();
		}
	}

	// https://stackoverflow.com/questions/719020/is-there-an-async-version-of-directoryinfo-getfiles-directory-getdirectories-i
	public static class DirectoryAsync
	{
		public static Task<System.IO.DirectoryInfo> GetParentAsync(String path)
		{ return Task.Run(() => { return Directory.GetParent(path); }); }
		public static Task<DirectoryInfo> CreateDirectoryAsync(String path)
		{ return Task.Run(() => { return Directory.CreateDirectory(path); }); }
		public static Task<DirectoryInfo> CreateDirectoryAsync(String path, System.Security.AccessControl.DirectorySecurity directorySecurity)
		{ return Task.Run(() => { return Directory.CreateDirectory(path, directorySecurity); }); }
		public static Task<Boolean> ExistsAsync(String path)
		{ return Task.Run(() => { return Directory.Exists(path); }); }
		public static Task SetCreationTimeAsync(String path, DateTime creationTime)
		{ return Task.Run(() => { Directory.SetCreationTime(path, creationTime); }); }
		public static Task SetCreationTimeUtcAsync(String path, DateTime creationTimeUtc)
		{ return Task.Run(() => { Directory.SetCreationTimeUtc(path, creationTimeUtc); }); }
		public static Task<DateTime> GetCreationTimeAsync(String path)
		{ return Task.Run(() => { return Directory.GetCreationTime(path); }); }
		public static Task<DateTime> GetCreationTimeUtcAsync(String path)
		{ return Task.Run(() => { return Directory.GetCreationTimeUtc(path); }); }
		public static Task SetLastWriteTimeAsync(String path, DateTime lastWriteTime)
		{ return Task.Run(() => { Directory.SetLastWriteTime(path, lastWriteTime); }); }
		public static Task SetLastWriteTimeUtcAsync(String path, DateTime lastWriteTimeUtc)
		{ return Task.Run(() => { Directory.SetLastWriteTimeUtc(path, lastWriteTimeUtc); }); }
		public static Task<DateTime> GetLastWriteTimeAsync(String path)
		{ return Task.Run(() => { return Directory.GetLastWriteTime(path); }); }
		public static Task<DateTime> GetLastWriteTimeUtcAsync(String path)
		{ return Task.Run(() => { return Directory.GetLastWriteTimeUtc(path); }); }
		public static Task SetLastAccessTimeAsync(String path, DateTime lastAccessTime)
		{ return Task.Run(() => { Directory.SetLastAccessTime(path, lastAccessTime); }); }
		public static Task SetLastAccessTimeUtcAsync(String path, DateTime lastAccessTimeUtc)
		{ return Task.Run(() => { Directory.SetLastAccessTimeUtc(path, lastAccessTimeUtc); }); }
		public static Task<DateTime> GetLastAccessTimeAsync(String path)
		{ return Task.Run(() => { return Directory.GetLastAccessTime(path); }); }
		public static Task<DateTime> GetLastAccessTimeUtcAsync(String path)
		{ return Task.Run(() => { return Directory.GetLastAccessTimeUtc(path); }); }
		public static Task<System.Security.AccessControl.DirectorySecurity> GetAccessControlAsync(String path)
		{ return Task.Run(() => { return Directory.GetAccessControl(path); }); }
		public static Task<System.Security.AccessControl.DirectorySecurity> GetAccessControlAsync(String path, System.Security.AccessControl.AccessControlSections includeSections)
		{ return Task.Run(() => { return Directory.GetAccessControl(path, includeSections); }); }
		public static Task SetAccessControlAsync(String path, System.Security.AccessControl.DirectorySecurity directorySecurity)
		{ return Task.Run(() => { Directory.SetAccessControl(path, directorySecurity); }); }
		public static Task<String[]> GetFilesAsync(String path)
		{ return Task.Run(() => { return Directory.GetFiles(path); }); }
		public static Task<String[]> GetFilesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.GetFiles(path, searchPattern); }); }
		public static Task<String[]> GetFilesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.GetFiles(path, searchPattern, searchOption); }); }
		public static Task<String[]> GetDirectoriesAsync(String path)
		{ return Task.Run(() => { return Directory.GetDirectories(path); }); }
		public static Task<String[]> GetDirectoriesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.GetDirectories(path, searchPattern); }); }
		public static Task<String[]> GetDirectoriesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.GetDirectories(path, searchPattern, searchOption); }); }
		public static Task<String[]> GetFileSystemEntriesAsync(String path)
		{ return Task.Run(() => { return Directory.GetFileSystemEntries(path); }); }
		public static Task<String[]> GetFileSystemEntriesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.GetFileSystemEntries(path, searchPattern); }); }
		public static Task<String[]> GetFileSystemEntriesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.GetFileSystemEntries(path, searchPattern, searchOption); }); }
		public static Task<IEnumerable<String>> EnumerateDirectoriesAsync(String path)
		{ return Task.Run(() => { return Directory.EnumerateDirectories(path); }); }
		public static Task<IEnumerable<String>> EnumerateDirectoriesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.EnumerateDirectories(path, searchPattern); }); }
		public static Task<IEnumerable<String>> EnumerateDirectoriesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.EnumerateDirectories(path, searchPattern, searchOption); }); }
		public static Task<IEnumerable<String>> EnumerateFilesAsync(String path)
		{ return Task.Run(() => { return Directory.EnumerateFiles(path); }); }
		public static Task<IEnumerable<String>> EnumerateFilesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.EnumerateFiles(path, searchPattern); }); }
		public static Task<IEnumerable<String>> EnumerateFilesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.EnumerateFiles(path, searchPattern, searchOption); }); }
		public static Task<IEnumerable<String>> EnumerateFileSystemEntriesAsync(String path)
		{ return Task.Run(() => { return Directory.EnumerateFileSystemEntries(path); }); }
		public static Task<IEnumerable<String>> EnumerateFileSystemEntriesAsync(String path, String searchPattern)
		{ return Task.Run(() => { return Directory.EnumerateFileSystemEntries(path, searchPattern); }); }
		public static Task<IEnumerable<String>> EnumerateFileSystemEntriesAsync(String path, String searchPattern, SearchOption searchOption)
		{ return Task.Run(() => { return Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption); }); }
		public static Task<String[]> GetLogicalDrivesAsync()
		{ return Task.Run(() => { return Directory.GetLogicalDrives(); }); }
		public static Task<String> GetDirectoryRootAsync(String path)
		{ return Task.Run(() => { return Directory.GetDirectoryRoot(path); }); }
		public static Task<String> GetCurrentDirectoryAsync()
		{ return Task.Run(() => { return Directory.GetCurrentDirectory(); }); }
		public static Task SetCurrentDirectoryAsync(String path)
		{ return Task.Run(() => { Directory.SetCurrentDirectory(path); }); }
		public static Task MoveAsync(String sourceDirName, String destDirName)
		{ return Task.Run(() => { Directory.Move(sourceDirName, destDirName); }); }
		public static Task DeleteAsync(String path)
		{ return Task.Run(() => { Directory.Delete(path); }); }
		public static Task DeleteAsync(String path, Boolean recursive)
		{ return Task.Run(() => { Directory.Delete(path, recursive); }); }
	}
	public static class FileAsync
	{
		public static Task<StreamReader> OpenTextAsync(String path)
		{ return Task.Run(() => { return File.OpenText(path); }); }
		public static Task<StreamWriter> CreateTextAsync(String path)
		{ return Task.Run(() => { return File.CreateText(path); }); }
		public static Task<StreamWriter> AppendTextAsync(String path)
		{ return Task.Run(() => { return File.AppendText(path); }); }
		public static Task CopyAsync(String sourceFileName, String destFileName)
		{ return Task.Run(() => { File.Copy(sourceFileName, destFileName); }); }
		public static Task CopyAsync(String sourceFileName, String destFileName, Boolean overwrite)
		{ return Task.Run(() => { File.Copy(sourceFileName, destFileName, overwrite); }); }
		public static Task<FileStream> CreateAsync(String path)
		{ return Task.Run(() => { return File.Create(path); }); }
		public static Task<FileStream> CreateAsync(String path, Int32 bufferSize)
		{ return Task.Run(() => { return File.Create(path, bufferSize); }); }
		public static Task<FileStream> CreateAsync(String path, Int32 bufferSize, FileOptions options)
		{ return Task.Run(() => { return File.Create(path, bufferSize, options); }); }
		public static Task<FileStream> CreateAsync(String path, Int32 bufferSize, FileOptions options, System.Security.AccessControl.FileSecurity fileSecurity)
		{ return Task.Run(() => { return File.Create(path, bufferSize, options, fileSecurity); }); }
		public static Task DeleteAsync(String path)
		{ return Task.Run(() => { File.Delete(path); }); }
		public static Task DecryptAsync(String path)
		{ return Task.Run(() => { File.Decrypt(path); }); }
		public static Task EncryptAsync(String path)
		{ return Task.Run(() => { File.Encrypt(path); }); }
		public static Task<Boolean> ExistsAsync(String path)
		{ return Task.Run(() => { return File.Exists(path); }); }
		public static Task<FileStream> OpenAsync(String path, FileMode mode)
		{ return Task.Run(() => { return File.Open(path, mode); }); }
		public static Task<FileStream> OpenAsync(String path, FileMode mode, FileAccess access)
		{ return Task.Run(() => { return File.Open(path, mode, access); }); }
		public static Task<FileStream> OpenAsync(String path, FileMode mode, FileAccess access, FileShare share)
		{ return Task.Run(() => { return File.Open(path, mode, access, share); }); }
		public static Task SetCreationTimeAsync(String path, DateTime creationTime)
		{ return Task.Run(() => { File.SetCreationTime(path, creationTime); }); }
		public static Task SetCreationTimeUtcAsync(String path, DateTime creationTimeUtc)
		{ return Task.Run(() => { File.SetCreationTimeUtc(path, creationTimeUtc); }); }
		public static Task<DateTime> GetCreationTimeAsync(String path)
		{ return Task.Run(() => { return File.GetCreationTime(path); }); }
		public static Task<DateTime> GetCreationTimeUtcAsync(String path)
		{ return Task.Run(() => { return File.GetCreationTimeUtc(path); }); }
		public static Task SetLastAccessTimeAsync(String path, DateTime lastAccessTime)
		{ return Task.Run(() => { File.SetLastAccessTime(path, lastAccessTime); }); }
		public static Task SetLastAccessTimeUtcAsync(String path, DateTime lastAccessTimeUtc)
		{ return Task.Run(() => { File.SetLastAccessTimeUtc(path, lastAccessTimeUtc); }); }
		public static Task<DateTime> GetLastAccessTimeAsync(String path)
		{ return Task.Run(() => { return File.GetLastAccessTime(path); }); }
		public static Task<DateTime> GetLastAccessTimeUtcAsync(String path)
		{ return Task.Run(() => { return File.GetLastAccessTimeUtc(path); }); }
		public static Task SetLastWriteTimeAsync(String path, DateTime lastWriteTime)
		{ return Task.Run(() => { File.SetLastWriteTime(path, lastWriteTime); }); }
		public static Task SetLastWriteTimeUtcAsync(String path, DateTime lastWriteTimeUtc)
		{ return Task.Run(() => { File.SetLastWriteTimeUtc(path, lastWriteTimeUtc); }); }
		public static Task<DateTime> GetLastWriteTimeAsync(String path)
		{ return Task.Run(() => { return File.GetLastWriteTime(path); }); }
		public static Task<DateTime> GetLastWriteTimeUtcAsync(String path)
		{ return Task.Run(() => { return File.GetLastWriteTimeUtc(path); }); }
		public static Task<FileAttributes> GetAttributesAsync(String path)
		{ return Task.Run(() => { return File.GetAttributes(path); }); }
		public static Task SetAttributesAsync(String path, FileAttributes fileAttributes)
		{ return Task.Run(() => { File.SetAttributes(path, fileAttributes); }); }
		public static Task<System.Security.AccessControl.FileSecurity> GetAccessControlAsync(String path)
		{ return Task.Run(() => { return File.GetAccessControl(path); }); }
		public static Task<System.Security.AccessControl.FileSecurity> GetAccessControlAsync(String path, System.Security.AccessControl.AccessControlSections includeSections)
		{ return Task.Run(() => { return File.GetAccessControl(path, includeSections); }); }
		public static Task SetAccessControlAsync(String path, System.Security.AccessControl.FileSecurity fileSecurity)
		{ return Task.Run(() => { File.SetAccessControl(path, fileSecurity); }); }
		public static Task<FileStream> OpenReadAsync(String path)
		{ return Task.Run(() => { return File.OpenRead(path); }); }
		public static Task<FileStream> OpenWriteAsync(String path)
		{ return Task.Run(() => { return File.OpenWrite(path); }); }
		public static Task<String> ReadAllTextAsync(String path)
		{ return Task.Run(() => { return File.ReadAllText(path); }); }
		public static Task<String> ReadAllTextAsync(String path, System.Text.Encoding encoding)
		{ return Task.Run(() => { return File.ReadAllText(path, encoding); }); }
		public static Task WriteAllTextAsync(String path, String contents)
		{ return Task.Run(() => { File.WriteAllText(path, contents); }); }
		public static Task WriteAllTextAsync(String path, String contents, System.Text.Encoding encoding)
		{ return Task.Run(() => { File.WriteAllText(path, contents, encoding); }); }
		public static Task<Byte[]> ReadAllBytesAsync(String path)
		{ return Task.Run(() => { return File.ReadAllBytes(path); }); }
		public static Task WriteAllBytesAsync(String path, Byte[] bytes)
		{ return Task.Run(() => { File.WriteAllBytes(path, bytes); }); }
		public static Task<String[]> ReadAllLinesAsync(String path)
		{ return Task.Run(() => { return File.ReadAllLines(path); }); }
		public static Task<String[]> ReadAllLinesAsync(String path, System.Text.Encoding encoding)
		{ return Task.Run(() => { return File.ReadAllLines(path, encoding); }); }
		public static Task<IEnumerable<String>> ReadLinesAsync(String path)
		{ return Task.Run(() => { return File.ReadLines(path); }); }
		public static Task<IEnumerable<String>> ReadLinesAsync(String path, System.Text.Encoding encoding)
		{ return Task.Run(() => { return File.ReadLines(path, encoding); }); }
		public static Task WriteAllLinesAsync(String path, String[] contents)
		{ return Task.Run(() => { File.WriteAllLines(path, contents); }); }
		public static Task WriteAllLinesAsync(String path, String[] contents, System.Text.Encoding encoding)
		{ return Task.Run(() => { File.WriteAllLines(path, contents, encoding); }); }
		public static Task WriteAllLinesAsync(String path, IEnumerable<String> contents)
		{ return Task.Run(() => { File.WriteAllLines(path, contents); }); }
		public static Task WriteAllLinesAsync(String path, IEnumerable<String> contents, System.Text.Encoding encoding)
		{ return Task.Run(() => { File.WriteAllLines(path, contents, encoding); }); }
		public static Task AppendAllTextAsync(String path, String contents)
		{ return Task.Run(() => { File.AppendAllText(path, contents); }); }
		public static Task AppendAllTextAsync(String path, String contents, System.Text.Encoding encoding)
		{ return Task.Run(() => { File.AppendAllText(path, contents, encoding); }); }
		public static Task AppendAllLinesAsync(String path, IEnumerable<String> contents)
		{ return Task.Run(() => { File.AppendAllLines(path, contents); }); }
		public static Task AppendAllLinesAsync(String path, IEnumerable<String> contents, System.Text.Encoding encoding)
		{ return Task.Run(() => { File.AppendAllLines(path, contents, encoding); }); }
		public static Task MoveAsync(String sourceFileName, String destFileName)
		{ return Task.Run(() => { File.Move(sourceFileName, destFileName); }); }
		public static Task ReplaceAsync(String sourceFileName, String destinationFileName, String destinationBackupFileName)
		{ return Task.Run(() => { File.Replace(sourceFileName, destinationFileName, destinationBackupFileName); }); }
		public static Task ReplaceAsync(String sourceFileName, String destinationFileName, String destinationBackupFileName, Boolean ignoreMetadataErrors)
		{ return Task.Run(() => { File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors); }); }
	}
}

