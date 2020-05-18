using nut.Entities;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace tests.nut.Entites
{
    public class PackagesParserTests
    {
        [Theory]
        [InlineData("example.csproj", 26)]
        public async Task PackagesParserTests_Correct_Ok
        (
            string fileName,
            int expectedPackagesCount
        )
        {
            //Given
            var filePath = Path.Combine
            (
                AppDomain.CurrentDomain.BaseDirectory,
                "Entities",
                "TestFiles",
                fileName
            );

            var csprojContent = await File.ReadAllTextAsync(filePath);
        
            //When
            var packages = PackagesParser.Parse(csprojContent);
        
            //Then
            Assert.Equal(expectedPackagesCount, packages.Length);
        }
    }
}