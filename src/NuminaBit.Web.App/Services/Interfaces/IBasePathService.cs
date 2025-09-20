namespace NuminaBit.Web.App.Services.Interfaces
{
    public interface IBasePathService
    {
        public string BasePath { get; }
        public void Init(HostType basePath);
        public string GetFullPath(string relativePath);
    }
}
