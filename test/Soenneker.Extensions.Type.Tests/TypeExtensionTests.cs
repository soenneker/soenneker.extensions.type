
namespace Soenneker.Extensions.Type.Tests;

public class TypeExtensionTests
{
    [Test]
    public async System.Threading.Tasks.Task ConvertPropertyValue_InvalidArrayElement_ReturnsNull()
    {
        object? result = typeof(int[]).ConvertPropertyValue("1,invalid,3");

        await Assert.That(result).IsNull();
    }
}
