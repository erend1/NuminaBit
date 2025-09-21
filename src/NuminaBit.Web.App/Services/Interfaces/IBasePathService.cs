namespace NuminaBit.Web.App.Services.Interfaces
{
    public interface IBasePathService
    {
        public string BasePath { get; }
        public bool Initialize();
        public string GetFullPath(string relativePath);
    }
}
