using Domain;
using Infrastructure.Runtime;
using NUnit.Framework;

namespace Infrastructure.Tests
{
    [TestFixture]
    public class PseudoRandomSourceTests
    {
        private const float TOLERANCE = 0.00001f;

        [Test]
        public void UnitFloat_WithSameIntegerInputAndUnchangedSeed_ShouldAlwaysReturnSameValue()
        {
            // Arrange
            const int INPUT_VALUE = 123;
            const int SEED = 1234;
            var valueSource = new PseudoRandomSource(SEED);

            // Act
            var firstValue = valueSource.UnitFloat(INPUT_VALUE);
            var secondValue = valueSource.UnitFloat(INPUT_VALUE);

            // Assert
            Assert.AreEqual(firstValue, secondValue, TOLERANCE);
        }

        [Test]
        public void UnitFloat_WithSameTupleInputAndUnchangedSeed_ShouldAlwaysReturnSameValue()
        {
            // Arrange
            const int X = 123;
            const int Y = 123;
            const int SEED = 1234;
            var valueSource = new PseudoRandomSource(SEED);

            // Act
            var firstValue = valueSource.UnitFloat(X, Y);
            var secondValue = valueSource.UnitFloat(X, Y);

            // Assert
            Assert.AreEqual(firstValue, secondValue, TOLERANCE);
        }

        [Test]
        public void UnitFloat_WithSameCellInputAndUnchangedSeed_ShouldAlwaysReturnSameValue()
        {
            // Arrange
            var inputValue = new IntegerPoint(123, 123);
            const int SEED = 1234;
            var valueSource = new PseudoRandomSource(SEED);

            // Act
            var firstValue = valueSource.UnitFloat(inputValue);
            var secondValue = valueSource.UnitFloat(inputValue);

            // Assert
            Assert.AreEqual(firstValue, secondValue, TOLERANCE);
        }

        [Test]
        public void NextUnitFloat_WithUnchangedSeedAndReset_ShouldProduceSameValueSequence()
        {
            // Arrange
            const int SEED = 1234;
            const int NUMBER = 5;
            var valueSource = new PseudoRandomSource(SEED);
            var firstList = new float[NUMBER];
            var secondList = new float[NUMBER];

            // Act
            for (var id = 0; id < NUMBER; id++)
                firstList[id] = valueSource.NextUnitFloat();

            valueSource.Reset(SEED);

            for (var id = 0; id < NUMBER; id++)
                secondList[id] = valueSource.NextUnitFloat();

            // Assert
            for (var id = 0; id < NUMBER; id++)
                Assert.AreEqual(firstList[id], secondList[id], TOLERANCE);
        }
    }
}