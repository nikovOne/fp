﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TagsCloudVisualization;

namespace TagsCloudVisualizationTests
{
    [TestFixture]
    public class CircularCloudLayouterTests
    {
        [SetUp]
        public void SetUp()
        {
            cloudLayouter = new CircularCloudLayouter(new Point(500, 500));
            rectangles = new List<Rectangle>();
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status is TestStatus.Failed)
            {
                var image = new Bitmap(800, 800);
                var brush = Graphics.FromImage(image);

                DrawRectangles(rectangles, brush);

                brush.DrawEllipse(new Pen(Color.Red, 3), 500, 500, 3, 3);
                image.Save(
                    $"{TestContext.CurrentContext.TestDirectory}\\{TestContext.CurrentContext.Test.Name}_result.png");
                TestContext.Write(
                    $"Tag cloud visualization saved to file {TestContext.CurrentContext.TestDirectory}\\{TestContext.CurrentContext.Test.Name}_result.png");
            }
        }

        private CircularCloudLayouter cloudLayouter;
        private List<Rectangle> rectangles;

        private static void DrawRectangles(IReadOnlyList<Rectangle> rectangles, Graphics brush)
        {
            for (var i = 0; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];
                brush.FillRectangle(new SolidBrush(Color.Green), rectangle);
                brush.DrawRectangle(new Pen(Color.Black), rectangle);
                brush.DrawString(i.ToString(), new Font(FontFamily.GenericMonospace, 12),
                    new SolidBrush(Color.Red), rectangle);
            }
        }

        [Test]
        public void PullNextRectangle_ShouldReturn_NotIntersectedRects()
        {
            var random = new Random(10);

            for (var i = 0; i < 100; i++)
                rectangles.Add(cloudLayouter.PutNextRectangle(new Size(random.Next(5, 70), random.Next(5, 70)))
                    .GetValueOrThrow());

            for (var i = 0; i < rectangles.Count - 1; i++)
            for (var j = i + 1; j < rectangles.Count; j++)
                rectangles[i].IntersectsWith(rectangles[j]).Should().BeFalse();
        }

        [TestCase(-10, 10, TestName = "Negative width")]
        [TestCase(10, -10, TestName = "Negative height")]
        [TestCase(0, 10, TestName = "Zero width")]
        [TestCase(10, 0, TestName = "Zero height")]
        public void PullNextRectangle_ShouldReturnUnsuccessfulResult_WhenSizeIncorrect(int width, int height)
        {
            var actual = cloudLayouter.PutNextRectangle(new Size(width, height));

            actual.IsSuccess.Should().BeFalse();
        }

        [Test]
        public void CloudLayouter_ShouldMustLeaveLessThan20PercentsEmptySpace_WhenSizesRandom()
        {
            var random = new Random(10);
            var center = cloudLayouter.GetCenter().GetValueOrThrow();

            for (var i = 0; i < 100; i++)
                rectangles.Add(cloudLayouter.PutNextRectangle(new Size(random.Next(5, 70), random.Next(5, 70)))
                    .GetValueOrThrow());

            var averageDistanceFromCenterToBorder = GetAverageDistanceFromCenterToBorder(center, rectangles);
            var areaOfCircle = Math.PI * Math.Pow(averageDistanceFromCenterToBorder, 2);
            var filedArea = rectangles.Select(x => x.Width * x.Height).Sum();
            var emptySpacePercents = (areaOfCircle - filedArea) / areaOfCircle * 100;

            emptySpacePercents.Should().BeLessThan(20);
        }

        private static double GetDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        private static int GetAverageDistanceFromCenterToBorder(Point center, IReadOnlyCollection<Rectangle> rectangles)
        {
            var sum = 0;

            foreach (var rectangle in rectangles)
            {
                sum += (int)GetDistance(center.X, center.Y, rectangle.X, rectangle.Y);
                sum += (int)GetDistance(center.X, center.Y, rectangle.Right, rectangle.Y);
                sum += (int)GetDistance(center.X, center.Y, rectangle.Right, rectangle.Bottom);
                sum += (int)GetDistance(center.X, center.Y, rectangle.X, rectangle.Bottom);
            }

            return sum / (rectangles.Count * 4);
        }
    }
}