using NUnit.Framework;
using AIInGames.Planning.Unity;
using AIInGames.Planning.Unity.Editor.Services;
using UnityEngine;

namespace AIInGames.Planning.Unity.Editor.Tests
{
    /// <summary>
    /// Unit tests for TypeValidator service.
    /// Tests business logic in isolation without UI dependencies.
    /// </summary>
    public class TypeValidatorTests
    {
        private DomainAsset _domain;
        private TypeValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _domain = ScriptableObject.CreateInstance<DomainAsset>();
            _validator = new TypeValidator();
        }

        [TearDown]
        public void TearDown()
        {
            if (_domain != null)
                Object.DestroyImmediate(_domain);
        }

        [Test]
        public void WouldCreateCircularDependency_DirectCircle_ReturnsTrue()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("A", "object"));

            // Act
            var result = _validator.WouldCreateCircularDependency(_domain, "A", "A");

            // Assert
            Assert.IsTrue(result, "Type cannot be its own parent");
        }

        [Test]
        public void WouldCreateCircularDependency_IndirectCircle_ReturnsTrue()
        {
            // Arrange: A -> B -> object
            _domain.Types.Add(new TypeDefinition("A", "B"));
            _domain.Types.Add(new TypeDefinition("B", "object"));

            // Act: Try to set B's parent to A (would create A -> B -> A)
            var result = _validator.WouldCreateCircularDependency(_domain, "B", "A");

            // Assert
            Assert.IsTrue(result, "Should detect indirect circular dependency");
        }

        [Test]
        public void WouldCreateCircularDependency_DeepChain_ReturnsTrue()
        {
            // Arrange: A -> B -> C -> object
            _domain.Types.Add(new TypeDefinition("A", "B"));
            _domain.Types.Add(new TypeDefinition("B", "C"));
            _domain.Types.Add(new TypeDefinition("C", "object"));

            // Act: Try to set C's parent to A
            var result = _validator.WouldCreateCircularDependency(_domain, "C", "A");

            // Assert
            Assert.IsTrue(result, "Should detect circular dependency in deep chain");
        }

        [Test]
        public void WouldCreateCircularDependency_ValidChange_ReturnsFalse()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("A", "object"));
            _domain.Types.Add(new TypeDefinition("B", "object"));

            // Act: A -> B is valid (no circle)
            var result = _validator.WouldCreateCircularDependency(_domain, "A", "B");

            // Assert
            Assert.IsFalse(result, "Should allow valid parent change");
        }

        [Test]
        public void WouldCreateCircularDependency_ObjectParent_ReturnsFalse()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("A", "B"));

            // Act
            var result = _validator.WouldCreateCircularDependency(_domain, "A", "object");

            // Assert
            Assert.IsFalse(result, "Setting parent to 'object' should always be valid");
        }

        [Test]
        public void ValidateTypeName_EmptyName_ReturnsError()
        {
            // Act
            var result = _validator.ValidateTypeName(_domain, "");

            // Assert
            Assert.IsNotNull(result, "Empty name should be invalid");
            Assert.That(result, Does.Contain("empty"));
        }

        [Test]
        public void ValidateTypeName_ReservedName_ReturnsError()
        {
            // Act
            var result = _validator.ValidateTypeName(_domain, "object");

            // Assert
            Assert.IsNotNull(result, "'object' is reserved");
            Assert.That(result, Does.Contain("reserved"));
        }

        [Test]
        public void ValidateTypeName_WithSpaces_ReturnsError()
        {
            // Act
            var result = _validator.ValidateTypeName(_domain, "my type");

            // Assert
            Assert.IsNotNull(result, "Spaces should not be allowed");
            Assert.That(result, Does.Contain("spaces"));
        }

        [Test]
        public void ValidateTypeName_StartsWithNumber_ReturnsError()
        {
            // Act
            var result = _validator.ValidateTypeName(_domain, "1type");

            // Assert
            Assert.IsNotNull(result, "Cannot start with number");
            Assert.That(result, Does.Contain("letter"));
        }

        [Test]
        public void ValidateTypeName_DuplicateName_ReturnsError()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("existing-type", "object"));

            // Act
            var result = _validator.ValidateTypeName(_domain, "existing-type");

            // Assert
            Assert.IsNotNull(result, "Duplicate names should be invalid");
            Assert.That(result, Does.Contain("already exists"));
        }

        [Test]
        public void ValidateTypeName_ValidName_ReturnsNull()
        {
            // Act
            var result = _validator.ValidateTypeName(_domain, "valid_type");

            // Assert
            Assert.IsNull(result, "Valid name should return null (no error)");
        }

        [Test]
        public void ValidateTypeName_RenameToSameName_AllowsWithExclude()
        {
            // Arrange
            var existingType = new TypeDefinition("my-type", "object");
            _domain.Types.Add(existingType);

            // Act: Renaming type to same name (should be allowed)
            var result = _validator.ValidateTypeName(_domain, "my-type", existingType);

            // Assert
            Assert.IsNull(result, "Should allow renaming to same name");
        }
    }
}
