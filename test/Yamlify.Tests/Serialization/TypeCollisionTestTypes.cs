namespace TypeCollision.NamespaceA
{
    /// <summary>
    /// Config class in NamespaceA for testing type name collisions.
    /// </summary>
    public class Config
    {
        public string? Setting { get; set; }
    }
}

namespace TypeCollision.NamespaceB
{
    /// <summary>
    /// Config class in NamespaceB for testing type name collisions.
    /// </summary>
    public class Config
    {
        public string? Value { get; set; }
        public int Level { get; set; }
    }
}
