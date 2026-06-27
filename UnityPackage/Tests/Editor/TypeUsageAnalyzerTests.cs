using NUnit.Framework;
using AIInGames.Planning.Unity;
using AIInGames.Planning.Unity.Editor.Services;
using UnityEngine;

namespace AIInGames.Planning.Unity.Editor.Tests
{
    /// <summary>
    /// Unit tests for TypeUsageAnalyzer service.
    /// </summary>
    public class TypeUsageAnalyzerTests
    {
        private DomainAsset _domain;
        private TypeUsageAnalyzer _analyzer;

        [SetUp]
        public void SetUp()
        {
            _domain = ScriptableObject.CreateInstance<DomainAsset>();
            _analyzer = new TypeUsageAnalyzer();
        }

        [TearDown]
        public void TearDown()
        {
            if (_domain != null)
                Object.DestroyImmediate(_domain);
        }

        [Test]
        public void FindTypeUsages_NoUsages_ReturnsEmptyList()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("unused-type", "object"));

            // Act
            var usages = _analyzer.FindTypeUsages(_domain, "unused-type");

            // Assert
            Assert.IsEmpty(usages, "Should find no usages");
        }

        [Test]
        public void FindTypeUsages_ChildType_FindsUsage()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("parent", "object"));
            _domain.Types.Add(new TypeDefinition("child", "parent"));

            // Act
            var usages = _analyzer.FindTypeUsages(_domain, "parent");

            // Assert
            Assert.AreEqual(1, usages.Count);
            Assert.That(usages[0], Does.Contain("child"));
            Assert.That(usages[0], Does.Contain("inherits"));
        }

        [Test]
        public void FindTypeUsages_InPredicate_FindsUsage()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("location", "object"));
            var predicate = new PredicateDefinition("at");
            predicate.Parameters.Add(new PredicateParameter("?obj", "object"));
            predicate.Parameters.Add(new PredicateParameter("?loc", "location"));
            _domain.Predicates.Add(predicate);

            // Act
            var usages = _analyzer.FindTypeUsages(_domain, "location");

            // Assert
            Assert.AreEqual(1, usages.Count);
            Assert.That(usages[0], Does.Contain("at"));
            Assert.That(usages[0], Does.Contain("Predicate"));
        }

        [Test]
        public void FindTypeUsages_InAction_FindsUsage()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("robot", "object"));
            var action = new ActionDefinition("move");
            action.Parameters.Add(new ActionParameter("?r", "robot"));
            action.Parameters.Add(new ActionParameter("?from", "location"));
            _domain.Actions.Add(action);

            // Act
            var usages = _analyzer.FindTypeUsages(_domain, "robot");

            // Assert
            Assert.AreEqual(1, usages.Count);
            Assert.That(usages[0], Does.Contain("move"));
            Assert.That(usages[0], Does.Contain("Action"));
        }

        [Test]
        public void FindTypeUsages_MultipleUsages_FindsAll()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("location", "object"));
            _domain.Types.Add(new TypeDefinition("room", "location")); // Child type

            var predicate = new PredicateDefinition("at");
            predicate.Parameters.Add(new PredicateParameter("?loc", "location"));
            _domain.Predicates.Add(predicate);

            var action = new ActionDefinition("move");
            action.Parameters.Add(new ActionParameter("?from", "location"));
            action.Parameters.Add(new ActionParameter("?to", "location"));
            _domain.Actions.Add(action);

            // Act
            var usages = _analyzer.FindTypeUsages(_domain, "location");

            // Assert
            Assert.AreEqual(3, usages.Count, "Should find child type + predicate + action");
        }

        [Test]
        public void WouldOrphanTypes_WithChildren_ReturnsTrue()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("parent", "object"));
            _domain.Types.Add(new TypeDefinition("child", "parent"));

            // Act
            var result = _analyzer.WouldOrphanTypes(_domain, "parent");

            // Assert
            Assert.IsTrue(result, "Deleting parent would orphan child");
        }

        [Test]
        public void WouldOrphanTypes_NoChildren_ReturnsFalse()
        {
            // Arrange
            _domain.Types.Add(new TypeDefinition("childless", "object"));

            // Act
            var result = _analyzer.WouldOrphanTypes(_domain, "childless");

            // Assert
            Assert.IsFalse(result, "No children to orphan");
        }
    }
}
