using NUnit.Framework;

namespace Domain.Tests
{
    [TestFixture]
    public class RectBoundsTests
    {
        [Test]
        [TestCase(-5, -4, 5, 4)]
        [TestCase(5, 4, -5, -4)]
        public void Ctor_WhenValuesGiven_ShouldBeSetCorrectly(int minX, int minY, int maxX, int maxY)
        {
            // Arrange
            const int MIN_X = -5;
            const int MIN_Y = -4;
            const int MAX_X = 5;
            const int MAX_Y = 4;

            // Act
            var testRect = new RectBounds(minX, minY, maxX, maxY);

            // Assert
            Assert.AreEqual(MIN_X, testRect.MinX);
            Assert.AreEqual(MAX_X, testRect.MaxX);
            Assert.AreEqual(MIN_Y, testRect.MinY);
            Assert.AreEqual(MAX_Y, testRect.MaxY);
        }

        [Test]
        public void WithinBounds_WhenPointIsWithinBoundsNoBuffer_ShouldReturnTrue()
        {
            // Arrange
            const int POINT_X = 2;
            const int POINT_Y = 3;
            var testPoint = new IntegerPoint(POINT_X, POINT_Y);
            var testBounds = new RectBounds(POINT_X - 2, POINT_Y - 2, POINT_X + 2, POINT_Y + 2);

            // Act
            var result = testBounds.WithinBounds(testPoint);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WithinBounds_WhenPointIsOutsideBoundsNoBuffer_ShouldReturnFalse()
        {
            // Arrange
            const int POINT_X = 2;
            const int POINT_Y = 3;
            var testPoint = new IntegerPoint(POINT_X, POINT_Y);
            var testBounds = new RectBounds(POINT_X - 4, POINT_Y - 4, POINT_X - 2, POINT_Y - 2);

            // Act
            var result = testBounds.WithinBounds(testPoint);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void WithinBounds_WhenPointIsWithinBoundsAndBuffer_ShouldReturnTrue()
        {
            // Arrange
            const int POINT_X = 2;
            const int POINT_Y = 3;
            const int BUFFER = 1;
            var testPoint = new IntegerPoint(POINT_X, POINT_Y);
            var testBounds = new RectBounds(POINT_X - 2, POINT_Y - 2, POINT_X + 2, POINT_Y + 2);

            // Act
            var result = testBounds.WithinBounds(testPoint, BUFFER);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WithinBounds_WhenPointIsWithinBoundsButInBufferZone_ShouldReturnFalse()
        {
            // Arrange
            const int POINT_X = 2;
            const int POINT_Y = 3;
            const int BUFFER = 2;
            var testPoint = new IntegerPoint(POINT_X, POINT_Y);
            var testBounds = new RectBounds(POINT_X - 2, POINT_Y - 2, POINT_X + 1, POINT_Y + 1);

            // Act
            var result = testBounds.WithinBounds(testPoint, BUFFER);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void WithinBounds_WhenPointIsOutsideBoundsButInBufferZone_ShouldReturnTrue()
        {
            // Arrange
            const int POINT_X = 2;
            const int POINT_Y = 3;
            const int BUFFER = 2;
            var testPoint = new IntegerPoint(POINT_X, POINT_Y);
            var testBounds = new RectBounds(POINT_X - 4, POINT_Y - 4, POINT_X - 1, POINT_Y - 1);

            // Act
            var result = testBounds.WithinBounds(testPoint, BUFFER);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Overlaps_WhenTwoRectsOverlapWithoutBuffer_ShouldReturnTrue()
        {
            // Arrange
            var rect1 = new RectBounds(1, 1, 3, 3);
            var rect2 = new RectBounds(3, 1, 6, 3);

            // Act
            var result = rect1.Overlaps(rect2);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Overlaps_WhenTwoRectsDoNotOverlapWithoutBuffer_ShouldReturnFalse()
        {
            // Arrange
            var rect1 = new RectBounds(1, 1, 3, 3);
            var rect2 = new RectBounds(4, 1, 6, 3);

            // Act
            var result = rect1.Overlaps(rect2);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Overlaps_WhenTwoRectsDoNotOverlapButDoInBuffer_ShouldReturnTrue()
        {
            // Arrange
            const int BUFFER = 1;
            var rect1 = new RectBounds(1, 1, 3, 3);
            var rect2 = new RectBounds(4, 1, 6, 3);

            // Act
            var result = rect1.Overlaps(rect2, BUFFER);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Overlaps_WhenTwoRectsOverlapButWithinNegativeBuffer_ShouldReturnFalse()
        {
            // Arrange
            const int BUFFER = -1;
            var rect1 = new RectBounds(1, 1, 3, 3);
            var rect2 = new RectBounds(3, 1, 6, 3);

            // Act
            var result = rect1.Overlaps(rect2, BUFFER);

            // Assert
            Assert.IsFalse(result);
        }
    }
}