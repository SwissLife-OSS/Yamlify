using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Tests for naming policy conversions.
/// </summary>
public class YamlNamingPolicyTests
{
    [Theory]
    [InlineData("PropertyName", "propertyName")]
    [InlineData("ID", "id")]
    [InlineData("XMLParser", "xmlParser")]
    public void CamelCaseConversion(string input, string expected)
    {
        var result = YamlNamingPolicy.CamelCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PropertyName", "property_name")]
    [InlineData("ID", "i_d")]
    [InlineData("IsActive", "is_active")]
    public void SnakeCaseConversion(string input, string expected)
    {
        var result = YamlNamingPolicy.SnakeCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PropertyName", "property-name")]
    [InlineData("ID", "i-d")]
    [InlineData("IsActive", "is-active")]
    public void KebabCaseConversion(string input, string expected)
    {
        var result = YamlNamingPolicy.KebabCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    public void CamelCase_SingleWord_NoChange(string input)
    {
        var result = YamlNamingPolicy.CamelCase.ConvertName(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    public void SnakeCase_SingleLowerWord_NoChange(string input)
    {
        var result = YamlNamingPolicy.SnakeCase.ConvertName(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    public void KebabCase_SingleLowerWord_NoChange(string input)
    {
        var result = YamlNamingPolicy.KebabCase.ConvertName(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("URLParser", "urlParser")]
    [InlineData("HTMLDocument", "htmlDocument")]
    [InlineData("APIVersion", "apiVersion")]
    public void CamelCase_MultipleUppercase(string input, string expected)
    {
        var result = YamlNamingPolicy.CamelCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("URLParser", "u_r_l_parser")]
    [InlineData("HTMLDocument", "h_t_m_l_document")]
    public void SnakeCase_MultipleUppercase(string input, string expected)
    {
        var result = YamlNamingPolicy.SnakeCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("URLParser", "u-r-l-parser")]
    [InlineData("HTMLDocument", "h-t-m-l-document")]
    public void KebabCase_MultipleUppercase(string input, string expected)
    {
        var result = YamlNamingPolicy.KebabCase.ConvertName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CamelCase_AlreadyCamelCase_NoChange()
    {
        var result = YamlNamingPolicy.CamelCase.ConvertName("propertyName");
        Assert.Equal("propertyName", result);
    }

    [Fact]
    public void SnakeCase_AlreadySnakeCase_NoChange()
    {
        var result = YamlNamingPolicy.SnakeCase.ConvertName("property_name");
        Assert.Equal("property_name", result);
    }

    [Fact]
    public void KebabCase_AlreadyKebabCase_NoChange()
    {
        var result = YamlNamingPolicy.KebabCase.ConvertName("property-name");
        Assert.Equal("property-name", result);
    }
}
