namespace Yamlify.Serialization;

internal sealed class CamelCaseNamingPolicy : YamlNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (!char.IsUpper(name[0])) return name;

        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsUpper(chars[i]))
            {
                break;
            }
            
            // If next char is lowercase, this is the start of a word - keep it uppercase (unless it's position 0)
            if (i > 0 && i + 1 < chars.Length && char.IsLower(chars[i + 1]))
            {
                break;
            }
            
            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }
}
