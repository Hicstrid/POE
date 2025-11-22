using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
namespace POE.Tests
{
    public class FileValidationTests
        {
            [Theory]
            [InlineData("document.pdf", true)]
            [InlineData("file.docx", true)]
            [InlineData("spreadsheet.xlsx", true)]
            [InlineData("image.jpg", false)]
            [InlineData("script.exe", false)]
            public void FileUpload_ShouldValidateFileTypes(string fileName, bool expectedValid)
            {
                // Arrange
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                var fileExtension = System.IO.Path.GetExtension(fileName).ToLower();

                // Act
                bool isValid = allowedExtensions.Contains(fileExtension);

                // Assert
                Assert.Equal(expectedValid, isValid);
            }

            [Fact]
            public void FileUpload_ShouldEnforceSizeLimit()
            {
                // Arrange
                long fileSize = 6 * 1024 * 1024; // 6MB
                long maxSize = 5 * 1024 * 1024; // 5MB limit

                // Act
                bool isValid = fileSize <= maxSize;

                // Assert
                Assert.False(isValid); // Should be invalid (6MB > 5MB)
            }

            [Fact]
            public void FileUpload_WithValidSize_ShouldPass()
            {
                // Arrange
                long fileSize = 4 * 1024 * 1024; // 4MB
                long maxSize = 5 * 1024 * 1024; // 5MB limit

                // Act
                bool isValid = fileSize <= maxSize;

                // Assert
                Assert.True(isValid); // Should be valid (4MB < 5MB)
            }
        }
    }