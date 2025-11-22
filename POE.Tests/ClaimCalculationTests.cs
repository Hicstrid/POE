using Xunit;
using POE.Models;

namespace POE.Tests
    {
        public class ClaimCalculationTests
        {
            [Fact]
            public void CalculateTotalAmount_ShouldReturnCorrectValue()
            {
                // Arrange
                decimal hoursWorked = 40;
                decimal hourlyRate = 150;

                // Act
                decimal totalAmount = hoursWorked * hourlyRate;

                // Assert
                Assert.Equal(6000, totalAmount);
            }

            [Theory]
            [InlineData(10, 100, 1000)]
            [InlineData(40, 150, 6000)]
            [InlineData(25.5, 200, 5100)]
            public void CalculateTotalAmount_TheoryTest(decimal hours, decimal rate, decimal expected)
            {
                // Act
                decimal result = hours * rate;

                // Assert
                Assert.Equal(expected, result);
            }

            [Fact]
            public void ClaimSubmit_WithValidData_ShouldBeValid()
            {
                // Arrange
                var claimSubmit = new ClaimSubmit
                {
                    HourlyRate = 150,
                    Details = new System.Collections.Generic.List<ClaimDetail>
                {
                    new ClaimDetail { HoursWorked = 10 },
                    new ClaimDetail { HoursWorked = 15 }
                }
                };

                // Act
                decimal totalHours = claimSubmit.Details.Sum(d => d.HoursWorked);
                decimal totalAmount = totalHours * claimSubmit.HourlyRate;

                // Assert
                Assert.Equal(25, totalHours);
                Assert.Equal(3750, totalAmount);
            }
        }
}
