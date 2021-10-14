using System;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Overwolf.Application.LogCollector
{
    public class LogCollector
    {
        internal readonly string overwolfAppLogFolder;

        public LogCollector()
        {
            overwolfAppLogFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Overwolf", "Log", "Apps");
        }

        public string ZipLogFiles(string appName)
        {
            GetCurrentLogfiles(appName, out var rootLogFolder);

            var tempFile = Path.GetTempFileName();
            // Deleting the file, so I can overwrite it
            File.Delete(tempFile);

            ZipFile.CreateFromDirectory(rootLogFolder, tempFile, CompressionLevel.NoCompression, false);

            File.Move(tempFile, tempFile.Replace(".tmp", ".zip"));

            return tempFile.Replace(".tmp", ".zip");
        }

        internal string[] GetCurrentLogfiles(string appName, out string rootLogFolder)
        {
            ValidateValidAppName(appName, out rootLogFolder);
            var logFiles = Directory.GetFiles(rootLogFolder, "*.*", SearchOption.AllDirectories);

            return logFiles;
        }

        internal void ValidateValidAppName(string appName, out string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new InvalidPathException("You need to specify the appName.");
            }

            var newPath = Path.Combine(overwolfAppLogFolder, appName);
            var parsedPath = Path.GetFullPath(newPath);

            if (!parsedPath.Contains(overwolfAppLogFolder))
            {
                throw new InvalidPathException("You're trying to access a folder, that this plugin was not meant to access.");
            }

            if (parsedPath == overwolfAppLogFolder)
            {
                throw new InvalidPathException("You're trying to access the app folder, you need to access a specific app folder.");
            }

            if (!Directory.Exists(parsedPath))
            {
                throw new InvalidPathException("You're trying to access a folder, that does not exist.");
            }

            if (!CanReadPath(parsedPath))
            {
                throw new InvalidPathException("You're trying to access a folder, that you do not have access to.");
            }

            logFilePath = parsedPath;
        }

        internal bool CanReadPath(string path)
        {
            try
            {
                bool readAllow = false, readDeny = false;
                var acl = Directory.GetAccessControl(path);

                if (acl == null)
                {
                    return false;
                }

                var currentUser = WindowsIdentity.GetCurrent();
                var aclRules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));

                if (aclRules == null)
                {
                    return false;
                }

                foreach (FileSystemAccessRule rule in aclRules)
                {
                    if (rule.IdentityReference == currentUser.User)
                    {
                        if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read)
                        {
                            continue;
                        }

                        if (rule.AccessControlType == AccessControlType.Allow)
                        {
                            readAllow = true;
                        }
                        else if (rule.AccessControlType == AccessControlType.Deny)
                        {
                            readDeny = true;
                        }
                    }
                }

                return readAllow && !readDeny;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
