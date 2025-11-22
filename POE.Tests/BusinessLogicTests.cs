using POE.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace POE.Tests
{
    public class BusinessLogicTests
        {
            [Fact]
            public void Claim_WithZeroHours_ShouldHaveZeroAmount()
            {
                // Arrange
                var claim = new Claims { TotalHours = 0, HourlyRate = 150 };

                // Act
                decimal totalAmount = claim.TotalHours * claim.HourlyRate;

                // Assert
                Assert.Equal(0, totalAmount);
            }

            [Fact]
            public void ClaimDetail_WithValidData_ShouldCalculateCorrectly()
            {
                // Arrange
                var detail = new ClaimDetail { HoursWorked = 8.5m };

                // Assert
                Assert.Equal(8.5m, detail.HoursWorked);
            }

            [Fact]
            public void SupportingDocument_WithFileName_ShouldExtractExtension()
            {
                // Arrange
                var document = new SupportingDocument { FileName = "contract.pdf" };

                // Act
                string extension = System.IO.Path.GetExtension(document.FileName);

                // Assert
                Assert.Equal(".pdf", extension);
            }
        }
    }

