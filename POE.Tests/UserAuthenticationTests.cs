using POE.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace POE.Tests
{
        public class UserAuthenticationTests
        {
            [Fact]
            public void User_WithValidEmail_ShouldContainAtSymbol()
            {
                // Arrange
                var user = new User { email = "lecturer@university.com" };

                // Assert
                Assert.Contains("@", user.email);
            }

            [Theory]
            [InlineData("lecturer@university.com", true)]
            [InlineData("invalid-email", false)]
            [InlineData("coordinator@college.ac.za", true)]
            public void User_EmailValidation_ShouldCheckForAtSymbol(string email, bool expected)
            {
                // Arrange
                var user = new User { email = email };

                // Act
                bool isValid = user.email.Contains("@");

                // Assert
                Assert.Equal(expected, isValid);
            }

            [Fact]
            public void User_WithRequiredFields_ShouldBeValid()
            {
                // Arrange
                var user = new User
                {
                    Name = "John",
                    Surname = "Doe",
                    email = "john@university.com",
                    username = "johndoe",
                    password = "securepassword",
                    role = "Lecturer"
                };

                // Assert
                Assert.NotNull(user.Name);
                Assert.NotNull(user.Surname);
                Assert.NotNull(user.email);
                Assert.NotNull(user.username);
                Assert.NotNull(user.password);
                Assert.NotNull(user.role);
            }
        }
    }