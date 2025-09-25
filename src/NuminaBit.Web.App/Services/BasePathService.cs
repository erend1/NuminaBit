using Microsoft.AspNetCore.Components;
using NuminaBit.Web.App.Services.Interfaces;

namespace NuminaBit.Web.App.Services
{

    public class BasePathService(NavigationManager nav) : IBasePathService
    {
        private enum HostType
        {
            Default,
            GitHub
        }

        private static readonly string _defaultBasePath = "/";
        private static readonly string _defaultBasePathOnGitHub = "/NuminaBit/";

        private bool isInitialized = false;
        private HostType hostType = HostType.Default;
        private readonly NavigationManager _nav = nav;

        public string BasePath { get; private set; } = _defaultBasePath;

        private bool SetBasePath()
        {
            BasePath = hostType switch
            {
                HostType.Default => _defaultBasePath,
                HostType.GitHub => _defaultBasePathOnGitHub,
                _ => _defaultBasePath,
            };
            return true;
        }

        public bool Initialize()
        {
            hostType = _nav.BaseUri.Contains("github.io") ? HostType.GitHub : HostType.Default;
            isInitialized = SetBasePath();
            return isInitialized;
        }

        public string GetFullPath(string relativePath)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("BasePathService is not initialized. Call Initialize() before using this method.");
            }
            string trimmedBasePath = relativePath.TrimStart('/');
            return $"{BasePath}{trimmedBasePath}";
        }
    }
}
