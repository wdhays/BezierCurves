using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using MathNet.Numerics;

namespace Curve
{
    public class BezierCurve
    {
        //Private Data Members
        private List<Vector3> controlPoints;
        private int resolution;

        //Constructor.
        public BezierCurve()
        {
            controlPoints = new List<Vector3>();
            resolution = 10;
        }

        //Return a list of control points currently in the curve.
        public List<Vector3> GetControlPoints()
        {
            return controlPoints;
        }

        //Return the number of control points in the curve.
        public int GetNumControlPoints()
        {
            return controlPoints.Count;
        }

        //Return the curves current resolution.
        public int GetResulution()
        {
            return resolution;
        }

        //Set the resolution of the curve to the parameter value.
        public void SetResolution(int newResolution)
        {
            if (newResolution >= GetNumControlPoints() - 1)
                resolution = newResolution;
            else
                resolution = GetNumControlPoints() - 1;
        }

        //Check to see if the passed index is a valid index in the control polygon list.
        public bool validIndex(int index)
        {
            //If there are no points in the controlPoints vector.
            if (controlPoints.Count == 0)
                return false;
            //If the index is valid.
            if ((index >= 0) && (index < GetNumControlPoints()))
                return true;
            //Otherwise false.
            return false;
        }

        //Add a control point to the control polygon at the end of the list.
        public void AddPoint(Vector3 newPoint)
        {
            controlPoints.Add(newPoint);
        }

        //Insert a point into the control polygon at the the desired index.
        public bool InsertPoint(int index, Vector3 newPoint)
        {
            //If the index is valid add the new point and return true.
            if (validIndex(index))
            {
                //Insert the new point at the index.
                controlPoints.Insert(index, newPoint);
                return true;
            }
            else
                //If the index was not valid return false.
                return false;
        }

        //Remove a control point from the control polygon at the desired index.
        public bool RemovePoint(int index)
        {
            //If the index is valid remove the point and return true.
            if (validIndex(index))
            {
                //Insert the new point at the index.
                controlPoints.RemoveAt(index);
                return true;
            }
            else
                //If the index was not valid return false.
                return false;
        }

        //Determines if the parameter values are within any control points hitbox.
        public int SelectControlPointByLocation(float x, float y)
        {
            for (int i = 0; i < GetNumControlPoints(); i++)
            {
                //Get the contol points x and y values as integers.
                float pointX = controlPoints[i].x, pointY = controlPoints[i].y;
                //Find the min and max values for the points hitbox.
                float xMin = pointX - 1, xMax = pointX + 1;
                float yMin = pointY - 1, yMax = pointY + 1;
                //Determine if a parameter value are within a certain points hitbox(2x2)
                if ((x >= xMin) && (x <= xMax) && (y >= yMin) && (y <= yMax))
                {
                    //If a point is found the meets the criteria return its index.
                    return i;
                }
            }
            //If no point was found return a sentinel value;
            return -1;

        }

        //Calculate the points on a Bezier curve using the control points and DeCasteljau's algorithm.
        public List<Vector3> CalculateDeCasteljauPoints()
        {
            //Declare variables needed.
            float step = 1.0f / resolution;
            List<Vector3> deCasteljauPoints = new List<Vector3>();
            int numControlPoints = GetNumControlPoints();
            int N = numControlPoints - 1;
            //Prefrom a loop for each step in t.
            for (float t = 0; t <= 1; t += step)
            {
                //Create a temporary list for this interation.
                List<Vector3> current = new List<Vector3>(controlPoints);
                //Preform the DeCasteljau algorithm.
                for (int k = 1; k <= N; k++)
                {
                    for (int j = 0; j <= N - k; j++)
                    {
                        current[j] = (((1 - t) * current[j]) + (t * current[j + 1]));
                    }
                }
                //Save the first point.
                if (current.Count > 0)
                    deCasteljauPoints.Add(current.First());
            }
            //Ensure the end is clamped to the last control point.
            if (!controlPoints.Last().Equals(deCasteljauPoints.Last()))
                deCasteljauPoints.Add(controlPoints.Last());
            //Return the curve points.
            return deCasteljauPoints;
        }

        //Calculate an intermidiate control polygon based on the parameter t and level.
        public List<Vector3> CalculateIntermediateControlPolygon(float t, int level)
        {
            //Declare variables needed.
            int N = GetNumControlPoints() - 1;

            //Create a temporary list for this interation.
            List<Vector3> current = new List<Vector3>(controlPoints);
            List<Vector3> previous = new List<Vector3>();

            //Preform the DeCasteljau algorithm.
            for (int k = 1; k <= level; k++)
            {
                //Relplace the previous list with the current list and clear the current list.
                previous = new List<Vector3>(current);
                current.Clear();

                //Build the current list based on the previous' list elements.
                for (int j = 0; j <= N - k; j++)
                {
                    current.Add(((1 - t) * previous[j]) + (t * previous[j + 1]));
                }
            }

            //Return the intermediate control polygon for this t and level.
            return current;
        }

        //Calculate a single point on a Bezier curve using a value for t, the control points, and DeCasteljau's algorithm.
        public Vector3 CalculateSingleCurvePoint(float t)
        {
            //Declare variables needed.
            int N = GetNumControlPoints() - 1;

            //Create a temporary list for this interation.
            List<Vector3> current = new List<Vector3>(controlPoints);
            //Preform the DeCasteljau algorithm.
            for (int k = 1; k <= N; k++)
            {
                for (int j = 0; j <= N - k; j++)
                {
                    current[j] = (((1 - t) * current[j]) + (t * current[j + 1]));
                }
            }

            //Return the curve points.
            return current.First();
        }

        //Calculate the points on a Bezier curve using the control points and Bernstein polynomials.
        public List<Vector3> CalculateBernsteinPoints()
        {
            //Declare variables needed.
            float step = 1.0f / resolution;
            List<Vector3> bernsteinPoints = new List<Vector3>();
            int numControlPoints = GetNumControlPoints();
            int N = numControlPoints - 1;

            //Prefrom a loop for each step in t.
            for (float t = 0; t <= 1; t += step)
            {
                //Create a temporary list for this interation.
                List<Vector3> current = new List<Vector3>(controlPoints);
                //Create temporary variables for the summation
                float tempX = 0.0f;
                float tempY = 0.0f;
                float tempZ = 0.0f;

                //Calculate a curve point using the Bernstein basis polynomials and the control points.
                for (int i = 0; i < numControlPoints; i++)
                {
                    tempX += (float)(current[i].x * CalculateBinomialCoefficient(N, i) * Math.Pow(t, i) * Math.Pow((1.0 - t), N - i));
                    tempY += (float)(current[i].y * CalculateBinomialCoefficient(N, i) * Math.Pow(t, i) * Math.Pow((1.0 - t), N - i));
                    tempZ += (float)(current[i].z * CalculateBinomialCoefficient(N, i) * Math.Pow(t, i) * Math.Pow((1.0 - t), N - i));
                }

                //Add the calculated point to the final list for this value of t.
                bernsteinPoints.Add(new Vector3(tempX, tempY, tempZ));
            }
            if (!controlPoints.Last().Equals(bernsteinPoints.Last()))
                bernsteinPoints.Add(controlPoints.Last());
            //Return the curve points.
            return bernsteinPoints;
        }

        //Calculate the binomial coeffcient for the parameter N and K.
        //http://www.geeksforgeeks.org/space-and-time-efficient-binomial-coefficient/
        private long CalculateBinomialCoefficient(long N, long K)
        {
            //When N == K or K == 0 the coeffcient is 1.
            if (N == K || K == 0)
                return 1;
            //Everything is symmetric around N - K, so it is quicker to iterate over a smaller K than a larger one.
            if (K > N - K)
                K = N - K;

            long C = 1;
            // Calculate value of [n * (n-1) *---* (n-k+1)] / [k * (k-1) *----* 1]
            for (long i = 0; i < K; ++i)
            {
                C *= (N - i);
                C /= (i + 1);
            }

            //Return the calculated coeffcient.
            return C;
        }

        //Will return a list of Vector3 that represents the control polygon of the Bezier curve raised in degree by 1.
        public List<Vector3> BezierDegreeElevation(List<Vector3> originalControlPoints)
        {
            //Declare variables needed.
            float degreePlusOne = originalControlPoints.Count;
            int degree = originalControlPoints.Count - 1;
            List<Vector3> newControlPolygonPoints = new List<Vector3>();

            //Add the first control point from the original to the degree elevated list.
            newControlPolygonPoints.Add(originalControlPoints.First());

            //Loop over the original control points and preform the degree elevation calculations.
            for(int i = 1; i <= degree; i++)
            {
                float x = (((i / degreePlusOne) * originalControlPoints[i - 1].x) + ((1 - (i / degreePlusOne)) * originalControlPoints[i].x));
                float y = (((i / degreePlusOne) * originalControlPoints[i - 1].y) + ((1 - (i / degreePlusOne)) * originalControlPoints[i].y));
                newControlPolygonPoints.Add(new Vector3(x, y, 0.0f));
            }

            //Add the last control point from the original to the degree elevated list.
            newControlPolygonPoints.Add(originalControlPoints.Last());

            //Return the new control polygon.
            return newControlPolygonPoints;
        }

        ////Will return a list of Vector3 that represents the control polygon of the Bezier curve reduced in degree by 1.
        public List<Vector3> BezierDegreeReduction(List<Vector3> originalControlPoints)
        {
            //Declare variables needed.
            int oldDegree = originalControlPoints.Count - 1;
            int newDegree = oldDegree - 1;
            float N = oldDegree - 1;

            List<Vector3> newControlPolygonPoints = new List<Vector3>();
            List<Vector3> newControlPolygonPointsLeft = new List<Vector3>();
            List<Vector3> newControlPolygonPointsRight = new List<Vector3>();

            //Add the first point of the original curve to the control polygon of the left curve.
            newControlPolygonPointsLeft.Add(originalControlPoints.First());
            //Calculate the points for the left curve.
            for (int i = 1; i < oldDegree; i++)
            {
                float x = (((N + 1.0f) / (N + 1.0f - i)) * originalControlPoints[i].x) - ((i / (N + 1.0f - i)) * newControlPolygonPointsLeft[i - 1].x);
                float y = (((N + 1.0f) / (N + 1.0f - i)) * originalControlPoints[i].y) - ((i / (N + 1.0f - i)) * newControlPolygonPointsLeft[i - 1].y);
                newControlPolygonPointsLeft.Add(new Vector3(x, y, 0.0f));
            }

            //Initialize the right curve to have the correct number of vectors.
            for (int i = 0; i < oldDegree; i++)
            {
                newControlPolygonPointsRight.Add(new Vector3());
            }
            //Add the last point of the original curve to the control polygon of the right curve.
            newControlPolygonPointsRight[newDegree] = originalControlPoints.Last();
            //Calculate the points for the right curve.
            for (int i = newDegree; i > 0; i--)
            {
                float x = (((N + 1) / i) * originalControlPoints[i].x) - (((N + 1 - i) / i) * newControlPolygonPointsRight[i].x);
                float y = (((N + 1) / i) * originalControlPoints[i].y) - (((N + 1 - i) / i) * newControlPolygonPointsRight[i].y);
                newControlPolygonPointsRight[i - 1] = new Vector3(x, y, 0.0f);
            }

            newControlPolygonPoints.Add(newControlPolygonPointsLeft.First());
            //Find the average of the two sub curves.
            for (int i = 1; i < oldDegree - 1; i++)
            {
                float x = (newControlPolygonPointsLeft[i].x + newControlPolygonPointsRight[i].x) * 0.5f;
                float y = (newControlPolygonPointsLeft[i].y + newControlPolygonPointsRight[i].y) * 0.5f;
                newControlPolygonPoints.Add(new Vector3(x, y, 0.0f));
            }
            newControlPolygonPoints.Add(newControlPolygonPointsRight.Last());

            //Return the new control polygon.
            return newControlPolygonPoints;
        }
    }
}