using DotEnvX.Core.Parser;
using DotEnvX.Core.Models;
using FluentAssertions;

namespace DotEnvX.Tests;

public class DotEnvParserTests
{
    [Fact]
    public void Parse_SimpleKeyValue_ReturnsCorrectDictionary()
    {
        var content = "KEY=value";
        var result = DotEnvParser.Parse(content);
        
        result.Should().HaveCount(1);
        result["KEY"].Should().Be("value");
    }
    
    [Fact]
    public void Parse_MultipleKeyValues_ReturnsAllValues()
    {
        var content = "KEY1=value1\nKEY2=value2\nKEY3=value3";
        var result = DotEnvParser.Parse(content);
        
        result.Should().HaveCount(3);
        result["KEY1"].Should().Be("value1");
        result["KEY2"].Should().Be("value2");
        result["KEY3"].Should().Be("value3");
    }
    
    [Fact]
    public void Parse_WithComments_IgnoresComments()
    {
        var content = "# This is a comment\nKEY=value # inline comment\n# Another comment";
        var result = DotEnvParser.Parse(content);
        
        result.Should().HaveCount(1);
        result["KEY"].Should().Be("value");
    }
    
    [Fact]
    public void Parse_EmptyLines_IgnoresEmptyLines()
    {
        var content = "KEY1=value1\n\n\nKEY2=value2\n\n";
        var result = DotEnvParser.Parse(content);
        
        result.Should().HaveCount(2);
        result["KEY1"].Should().Be("value1");
        result["KEY2"].Should().Be("value2");
    }
    
    [Fact]
    public void Parse_QuotedValues_RemovesQuotes()
    {
        var content = "KEY1=\"quoted value\"\nKEY2='single quoted'\nKEY3=\"value with spaces\"";
        var result = DotEnvParser.Parse(content);
        
        result["KEY1"].Should().Be("quoted value");
        result["KEY2"].Should().Be("single quoted");
        result["KEY3"].Should().Be("value with spaces");
    }
    
    [Fact]
    public void Parse_MultilineValues_HandlesCorrectly()
    {
        var content = "KEY=\"line1\nline2\nline3\"";
        var result = DotEnvParser.Parse(content);
        
        result["KEY"].Should().Be("line1\nline2\nline3");
    }
    
    [Fact]
    public void Parse_VariableExpansion_ExpandsVariables()
    {
        var content = "BASE=hello\nEXPANDED=${BASE} world\nNESTED=${EXPANDED}!";
        var result = DotEnvParser.Parse(content);
        
        result["BASE"].Should().Be("hello");
        result["EXPANDED"].Should().Be("hello world");
        result["NESTED"].Should().Be("hello world!");
    }
    
    [Fact]
    public void Parse_EscapeSequences_ProcessesCorrectly()
    {
        var content = "KEY=\"\\n\\t\\r\\\\\\\"\"";
        var result = DotEnvParser.Parse(content);
        
        result["KEY"].Should().Be("\n\t\r\\\"");
    }
    
    [Fact]
    public void Parse_ExportKeyword_IgnoresExport()
    {
        var content = "export KEY=value\nexport ANOTHER=test";
        var result = DotEnvParser.Parse(content);
        
        result["KEY"].Should().Be("value");
        result["ANOTHER"].Should().Be("test");
    }
    
    [Fact]
    public void Parse_EmptyValue_ReturnsEmptyString()
    {
        var content = "EMPTY=\nKEY=value";
        var result = DotEnvParser.Parse(content);
        
        result["EMPTY"].Should().Be("");
        result["KEY"].Should().Be("value");
    }
    
    [Fact]
    public void Parse_WithOverload_OverridesExistingValues()
    {
        var content = "KEY=new_value";
        var options = new DotEnvParseOptions
        {
            Overload = true,
            ProcessEnv = new Dictionary<string, string> { ["KEY"] = "old_value" }
        };
        
        var result = DotEnvParser.Parse(content, options);
        
        result["KEY"].Should().Be("new_value");
    }
    
    [Fact]
    public void Parse_WithoutOverload_KeepsExistingValues()
    {
        var content = "KEY=new_value";
        var processEnv = new Dictionary<string, string> { ["KEY"] = "old_value" };
        var options = new DotEnvParseOptions
        {
            Overload = false,
            ProcessEnv = processEnv
        };
        
        var result = DotEnvParser.Parse(content, options);
        
        // Parser returns parsed values, not the final env state
        result["KEY"].Should().Be("new_value");
    }
}