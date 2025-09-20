using NuminaBit.Web.App.Services.Interfaces;

namespace NuminaBit.Web.App.Services
{
    public enum HostType
    {
        Default,
        GitHub
    }

    public class BasePathService: IBasePathService
    {
        private readonly string _defaultBasePath = "/";
        private readonly string _defaultBasePathOnGitHub = "/NuminaBit/";

        public string BasePath { get; private set; } = string.Empty;

        public string GetFullPath(string relativePath)
        {
            string trimmedBasePath = relativePath.TrimStart('/');
            return $"{BasePath}{trimmedBasePath}";
        }

        public void Init(HostType basePath)
        {
            BasePath = basePath switch
            {
                HostType.Default => _defaultBasePath,
                HostType.GitHub => _defaultBasePathOnGitHub,
                _ => _defaultBasePath,
            };
        }
    }
}
