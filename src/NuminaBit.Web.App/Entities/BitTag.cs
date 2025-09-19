namespace NuminaBit.Web.App.Entities
{
    public readonly record struct BitTag(string Scope, int Index)
    {
        public override string ToString() => $"{Scope}[{Index}]";
    }
}
