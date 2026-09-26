
namespace Soenneker.Extensions.Type.Tests;

public class TypeExtensionTests
{
    [Test]
    public async System.Threading.Tasks.Task ConvertPropertyValue_Array_PreservesElementTypeAndValues()
    {
        var result = (int[])typeof(int[]).ConvertPropertyValue("1,2,3")!;

        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That(result[0]).IsEqualTo(1);
        await Assert.That(result[2]).IsEqualTo(3);
    }

    [Test]
    public async System.Threading.Tasks.Task ConvertPropertyValue_InvalidArrayElement_ReturnsNull()
    {
        object? result = typeof(int[]).ConvertPropertyValue("1,invalid,3");

        await Assert.That(result).IsNull();
    }
}
