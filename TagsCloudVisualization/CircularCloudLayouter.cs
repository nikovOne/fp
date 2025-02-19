﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TagsCloudVisualization.Enums;
using TagsCloudVisualization.Interfaces;

namespace TagsCloudVisualization
{
    public class CircularCloudLayouter : ICloudLayouter
    {
        private readonly Point center;
        private readonly List<Rectangle> rectangles = new();
        private readonly int step;
        private int angle;

        public CircularCloudLayouter(Point center, int step = 10)
        {
            this.center = center;
            this.step = step;
            angle = 0;
        }

        public Result<Point> GetCenter()
        {
            return center;
        }

        public Result<Rectangle> PutNextRectangle(Size rectangleSize)
        {
            if (rectangleSize.Height <= 0 || rectangleSize.Width <= 0)
                return Result.Fail<Rectangle>(
                    $"Width and height should be greater than zero. Width: {rectangleSize.Width}, height: {rectangleSize.Height}");

            var nextRectangle = MoveToCenter(GetNextPossibleRectangle(rectangleSize));

            rectangles.Add(nextRectangle);
            return nextRectangle;
        }

        private Point GetNextPointAndUpdateAngle()
        {
            var length = step / (2 * Math.PI) * angle * Math.PI / 180;
            var x = (int)(length * Math.Cos(angle)) + center.X;
            var y = (int)(length * Math.Sin(angle)) + center.Y;
            angle++;

            return new Point(x, y);
        }

        private bool DoesIntersectPreviousRectangles(Rectangle rectangle)
        {
            return rectangles.Any(x => x.IntersectsWith(rectangle));
        }

        private static bool CanMadeMoveOnDistance(int distance)
        {
            return distance > 0 && distance != int.MaxValue;
        }

        private double GetDistanceFromCenter(Point point)
        {
            return Math.Sqrt(Math.Pow(center.X - point.X, 2) + Math.Pow(center.Y - point.Y, 2));
        }

        private Rectangle MoveToCenter(Rectangle previousRectangle)
        {
            var newRectangle = previousRectangle;

            while (true)
            {
                var possibleMovement = GetPossibleMovement(newRectangle);

                if (possibleMovement == null)
                    break;

                Point? newPoint = possibleMovement.MovementDirection switch
                {
                    MovementDirection.Down => new Point(newRectangle.X, newRectangle.Y + possibleMovement.Distance),
                    MovementDirection.Up => new Point(newRectangle.X, newRectangle.Y - possibleMovement.Distance),
                    MovementDirection.Left => new Point(newRectangle.X - possibleMovement.Distance, newRectangle.Y),
                    MovementDirection.Right => new Point(newRectangle.X + possibleMovement.Distance, newRectangle.Y),
                    _ => null
                };

                if (newPoint == null || GetDistanceFromCenter(newPoint.Value)
                    >= GetDistanceFromCenter(newRectangle.Location))
                    break;

                newRectangle.Location = newPoint.Value;
            }

            return newRectangle;
        }

        private Movement? GetPossibleMovement(Rectangle newRectangle)
        {
            var maxPossibleDistanceUp = int.MaxValue;
            var maxPossibleDistanceDown = int.MaxValue;
            var maxPossibleDistanceRight = int.MaxValue;
            var maxPossibleDistanceLeft = int.MaxValue;

            foreach (var rect in rectangles)
            {
                if (DoesSegmentsIntersect(rect.Left, rect.Right,
                    newRectangle.Left, newRectangle.Right))
                {
                    if (rect.Bottom <= newRectangle.Top)
                        maxPossibleDistanceUp =
                            Math.Min(newRectangle.Top - rect.Bottom, maxPossibleDistanceUp);
                    else if (rect.Top >= newRectangle.Bottom)
                        maxPossibleDistanceDown =
                            Math.Min(rect.Top - newRectangle.Bottom, maxPossibleDistanceDown);
                }

                if (DoesSegmentsIntersect(rect.Top, rect.Bottom,
                    newRectangle.Top, newRectangle.Bottom))
                {
                    if (rect.Right <= newRectangle.Left)
                        maxPossibleDistanceLeft =
                            Math.Min(newRectangle.Left - rect.Right, maxPossibleDistanceLeft);
                    else if (rect.Left >= newRectangle.Right)
                        maxPossibleDistanceRight = Math.Min(rect.Left - newRectangle.Right,
                            maxPossibleDistanceRight);
                }
            }

            if (CanMadeMoveOnDistance(maxPossibleDistanceDown))
                return new Movement(maxPossibleDistanceDown, MovementDirection.Down);
            if (CanMadeMoveOnDistance(maxPossibleDistanceUp))
                return new Movement(maxPossibleDistanceUp, MovementDirection.Up);
            if (CanMadeMoveOnDistance(maxPossibleDistanceLeft))
                return new Movement(maxPossibleDistanceLeft, MovementDirection.Left);
            if (CanMadeMoveOnDistance(maxPossibleDistanceRight))
                return new Movement(maxPossibleDistanceRight, MovementDirection.Right);

            return null;
        }


        private Rectangle GetNextPossibleRectangle(Size size)
        {
            Rectangle nextRectangle;

            do
            {
                var nextPoint = GetNextPointAndUpdateAngle();
                nextRectangle = new Rectangle(nextPoint, size);
            } while (DoesIntersectPreviousRectangles(nextRectangle));

            return nextRectangle;
        }

        private static bool DoesSegmentsIntersect(int firstSegmentStart, int firstSegmentEnd, int secondSegmentStart,
            int secondSegmentEnd)
        {
            return Math.Max(firstSegmentStart, secondSegmentStart) < Math.Min(firstSegmentEnd, secondSegmentEnd);
        }
    }
}