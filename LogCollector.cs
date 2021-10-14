using System;
using System.IO;
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

        internal string[] GetCurrentLogfiles(string appName)
        {
            ValidateValidAppName(appName, out var logFilePath);
            var logFiles = Directory.GetFiles(logFilePath, "*.*", SearchOption.AllDirectories);

            return logFiles;
        }

        internal void ValidateValidAppName(string appName, out string logFilePath)
        {
            var newPath = Path.Combine(overwolfAppLogFolder, appName);
            var parsedPath = Path.GetFullPath(newPath);

            if (!parsedPath.Contains(overwolfAppLogFolder))
            {
                throw new InvalidPathException("You're trying to access a folder, that this plugin was not meant to access.");
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
