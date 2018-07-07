using System.Collections.Generic;
using System.Linq;
using System;
using Curve;
using UnityEngine;
using UnityEngine.UI;

public class ProgramManager : MonoBehaviour
{
    //Public UI elements.
    public GameObject controlPointPrefab;
    public GameObject curveIntersectionPrefab;
    public Slider timeSlider;
    public Dropdown drawMethodDropDown;
    public Text curveInformation;
    public Text sliderValueText;
    public InputField xInput;
    public InputField yInput;
    public InputField indexInput;
    public InputField thresholdInput;
    public InputField degreeInput;

    //Private script variables.
    private List<BezierCurve> allCurves = new List<BezierCurve>();
    private List<Vector3> allCurveIntersections = new List<Vector3>();
    private int currentCurve = 0;
    private int drawMethod = 0;
    private int controlPointIndex = -1;

    //Run once at start up.
    private void Start()
    {
        //Draw grid lines on the screen.
        DrawGridLines();
    }

    //Run once each frame.
    private void Update()
    {
        //Destroy all old lines.
        DestroyGameObjectsWithTag("Line");
        //Destroy all old control points.
        DestroyGameObjectsWithTag("ControlPoint");
        //Destroy all old curve intersections.
        DestroyGameObjectsWithTag("CurveIntersection");
        //For each curve in the list draw the Bezier curve.
        DrawBezierCurves();
        //For each curve in the list draw the control polygon.
        DrawControlPolygons();
        //For each curve in the list create a prefab control point at the coordinates.
        DrawControlPoints();
        //Draw a special point on the current curve that reprsent the location at the time passed by the time slider.
        DrawCurvePoint();
        //Draw all intermediate contol polygons for the current curve.
        DrawIntermediateControlPolygon();
        //Display information about the current curve.
        DisplayCurveInformation();
        //Check to see if the user is clicking and dragging a control point.
        ControlPointDragAndDrop();
        //Add Points on mouse click
        AddControlPointsOnClick();
        //Draw an X at each curve intersection.
        DrawCurveIntersections();
    }

    //Create a prefab sphere at each control point that can be dragged and droped.
    private void DrawControlPoints()
    {
        for (int i = 0; i < allCurves.Count; i++)
        {
            for (int j = 0; j < allCurves[i].GetNumControlPoints(); j++)
            {
                float x = allCurves[i].GetControlPoints()[j].x;
                float y = allCurves[i].GetControlPoints()[j].y;
                float z = allCurves[i].GetControlPoints()[j].z;
                Instantiate(controlPointPrefab, new Vector3(x, y, z), Quaternion.Euler(0, 0, 0));
            }
        }
    }

    //Create a prefab X at each calculated curve intersection currently in the scene.
    private void DrawCurveIntersections()
    {
        for (int i = 0; i < allCurveIntersections.Count; i++)
        {
            float x = allCurveIntersections[i].x;
            float y = allCurveIntersections[i].y;
            float z = allCurveIntersections[i].z;
            Instantiate(curveIntersectionPrefab, new Vector3(x, y, z), Quaternion.Euler(0, 0, 0));
        }
    }

    //Create a prefab sphere at the t value for the current curve.
    private void DrawCurvePoint()
    {
        //If there are no curves then we have no current curve to work on.
        if (allCurves.Count > 0)
        {
            if (allCurves[currentCurve].GetNumControlPoints() > 1)
            {
                Vector3 curvePoint = new Vector3();
                curvePoint = allCurves[currentCurve].CalculateSingleCurvePoint(timeSlider.value);
                Instantiate(controlPointPrefab, curvePoint, Quaternion.Euler(0, 0, 0));
            }
        }
    }

    //Draw all control polygons.
    private void DrawControlPolygons()
    {
        //Create a default material, this will be over written later.
        Material curveMaterial = Resources.Load("Default", typeof(Material)) as Material;

        //Loop over all curves in the list.
        for (int i = 0; i < allCurves.Count; i++)
        {
            //Make sure that the curve has some control points to work with.
            if (allCurves[i].GetNumControlPoints() > 1)
            {
                //The currently selected control polygon will be drawn in a different color than all other control polygons.
                if (i == currentCurve)
                    curveMaterial = Resources.Load("CurrentCurve", typeof(Material)) as Material;
                else
                    curveMaterial = Resources.Load("NonCurrentCurve", typeof(Material)) as Material;

                //Draw the control polygon.
                for (int j = 0; j < allCurves[i].GetNumControlPoints() - 1; j++)
                {
                    DrawLine(allCurves[i].GetControlPoints()[j], allCurves[i].GetControlPoints()[j + 1], curveMaterial, 2, "Line", "ControlPolyline");
                }
            }
        }
    }

    //Draw all lines that make up the curve at its current resolution.
    private void DrawBezierCurves()
    {
        //Loop over all curves in the list.
        for (int i = 0; i < allCurves.Count; i++)
        {
            //Make sure that the curve has some control points to work with.
            if (allCurves[i].GetNumControlPoints() > 1)
            {
                //Calculate the curve points for each curve based on the control points.
                List<Vector3> curvePoints = new List<Vector3>();
                //Choose the draw method based on the user selection obtained from the drop down UI element.
                if (drawMethod == 0)
                    curvePoints = allCurves[i].CalculateDeCasteljauPoints();
                else
                    curvePoints = allCurves[i].CalculateBernsteinPoints();

                //Get the material we will use to draw the curve, this is just black.
                Material nonCurrentCurveMat = Resources.Load("Curve", typeof(Material)) as Material;

                //Draw the curve.
                for (int j = 0; j < curvePoints.Count - 1; j++)
                {
                    DrawLine(curvePoints[j], curvePoints[j + 1], nonCurrentCurveMat, 1, "Line", "CurveLine");
                }
            }
        }
    }

    //Draw all lines that make up the intermidiate control polygons for the current curve.
    private void DrawIntermediateControlPolygon()
    {
        //If there are no curves then we have no current curve to work on.
        if (allCurves.Count > 0)
        {
            //Declare variables and objects needed.
            List<Vector3> intermediateControlPolygonPoints = new List<Vector3>();
            int N = allCurves[currentCurve].GetNumControlPoints() - 1;

            //Call the DeCasteljau algorithm, each time effectively reduceing the number of edges in the control polygon by one each iteration.
            for (int level = 1; level <= N; level++)
            {
                //If (N - level) == 0 then DeCasteljau will only return one point, we need atleast 2 for an edge.
                if (N - level > 0)
                {
                    //Calculate the intermediate control points for each level, essentially stoping the DeCasteljau algorithm before it is complete.
                    intermediateControlPolygonPoints = allCurves[currentCurve].CalculateIntermediateControlPolygon(timeSlider.value, level);

                    //Draw the intermediate control polygon for this level.
                    for (int j = 0; j < intermediateControlPolygonPoints.Count - 1; j++)
                    {
                        //Get a different color for each intermediate control polygon.
                        //There are 8 different colors names 'Color1', 'Color2', etc, just use the modulus to choose one.
                        Material curveMaterial = Resources.Load("Color" + ((level - 1) % 8 + 1), typeof(Material)) as Material;
                        DrawLine(intermediateControlPolygonPoints[j], intermediateControlPolygonPoints[j + 1], curveMaterial, 0, "Line", "IntermediateControlPolyline");
                    }
                }
            }
        }
    }

    //If the user has chosen to subdivide the current curve calculate the new curves, add them to the scene and remove the original.
    public void SubdivideBezierCurve()
    {
        //If there are no curves then we have no current curve to work on, or the time slider value would give a boring result.
        if (allCurves.Count > 0 && 0.0f < timeSlider.value && timeSlider.value < 1.0f)
        {
            //Declare variables and objects needed.
            List<Vector3> intermediateControlPolygonPoints = new List<Vector3>();
            List<Vector3> leftCurve = new List<Vector3>();
            List<Vector3> rightCurve = new List<Vector3>();
            int N = allCurves[currentCurve].GetNumControlPoints() - 1;

            //Add the first at last points of the original control polygon to the appropriate curve.
            leftCurve.Add(allCurves[currentCurve].GetControlPoints().First());
            rightCurve.Add(allCurves[currentCurve].GetControlPoints().Last());

            //Call the DeCasteljau algorithm, each time effectively reduceing the number of edges in the control polygon by one each iteration.
            for (int level = 1; level <= N; level++)
            {
                //Calculate the intermediate control points for each level, essentially stoping the DeCasteljau algorithm before it is complete.
                intermediateControlPolygonPoints = allCurves[currentCurve].CalculateIntermediateControlPolygon(timeSlider.value, level);

                //If level == N then we only have one point, the curve point at t.
                if (level != N)
                {
                    //Add the first point of each intermediate control polygon to the end of the left curve.
                    leftCurve.Add(intermediateControlPolygonPoints.First());
                    //Add the last point of each intermediate control polygon to the begining of the right curve.
                    rightCurve.Insert(0, intermediateControlPolygonPoints.Last());
                }
                else
                {
                    //Add the curve point at t to the end of the left curve.
                    leftCurve.Add(intermediateControlPolygonPoints.First());
                    //Add the curve point at t to the begining of the right curve.
                    rightCurve.Insert(0, intermediateControlPolygonPoints.First());
                }
            }

            //Add the new right curve to the main curve list.
            BezierCurve newRightBezierCurve = new BezierCurve();
            newRightBezierCurve.SetResolution(allCurves[currentCurve].GetResulution());
            for (int j = 0; j < rightCurve.Count; j++)
                newRightBezierCurve.AddPoint(rightCurve[j]);
            allCurves.Insert(currentCurve, newRightBezierCurve);

            //Add the new left curve to the main curve list.
            BezierCurve newLeftBezierCurve = new BezierCurve();
            newLeftBezierCurve.SetResolution(allCurves[currentCurve].GetResulution());
            for (int j = 0; j < leftCurve.Count; j++)
                newLeftBezierCurve.AddPoint(leftCurve[j]);
            allCurves.Insert(currentCurve, newLeftBezierCurve);

            //Remove the original curve from the list.
            allCurves.RemoveAt(currentCurve + 2);
            //Reset the time slider's value.
            timeSlider.value = 0;
        }
    }

    //Draws a line between two points by creating a LineRenderer game object and setting up it's options.
    private void DrawLine(Vector3 v1, Vector3 v2, Material curveMaterial, int sortingOrder, string tag, string name)
    {
        //Create a new game object.
        GameObject myLine = new GameObject();
        //Give it a tag of 'line' and attach a line renderer component.
        myLine.gameObject.tag = tag;
        myLine.gameObject.name = name;
        myLine.AddComponent<LineRenderer>();
        //Get the newly created line renderer component.
        LineRenderer lineRenderer = myLine.GetComponent<LineRenderer>();
        //Set up the line renderer compnent.
        lineRenderer.material = curveMaterial;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.positionCount = 2;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        //Set the start and end points of the line to the parameter values.
        lineRenderer.SetPosition(0, v1);
        lineRenderer.SetPosition(1, v2);
    }

    //Handles dragging and dropping of control points in the current curve.
    public void ControlPointDragAndDrop()
    {
        //Make sure that there are curves and the current curve has control points to manipulate.
        if (allCurves.Count > 0 && allCurves[currentCurve].GetNumControlPoints() > 0)
        {
            //Make sure we are not trying to add new points, as in, not holding down shift.
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                //Declare variables needed.
                Vector3 objPosition = new Vector3();
                Vector3 mousePosition = new Vector3();

                //When the user clicks down the left mouse button find the index of the conrol point they want to drag, if any.
                if (Input.GetMouseButtonDown(0))
                {
                    //Get the screen coordinates of the mouse and convert them to world coordinates.
                    mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10.0f);
                    objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                    //Using the world coordinates attempt to find a control point in the current curve that is close, within +/- 1.
                    controlPointIndex = allCurves[currentCurve].SelectControlPointByLocation(objPosition.x, objPosition.y);
                }

                //If the left mouse button is begin held down and we have a valid index of a control point, translate that point.
                if (Input.GetMouseButton(0))
                {
                    if (controlPointIndex != -1)
                    {
                        //Get the screen coordinates of the mouse and convert them to world coordinates.
                        mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10.0f);
                        objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                        //Translate the selected point to the world coordinates.
                        allCurves[currentCurve].GetControlPoints()[controlPointIndex] = objPosition;
                    }
                }

                //When the user releases the mouse button reset the selected index to the sentinel value.
                if (Input.GetMouseButtonUp(0))
                {
                    controlPointIndex = -1;
                }
            }
        }
    }

    //Add a new control point to the current curve at the location on the screen clicked by the user.
    private void AddControlPointsOnClick()
    {
        //Make sure there is a curve to add points to.
        if (allCurves.Count > 0)
        {
            //The user must be holding down the shift key to add new points.
            if (Input.GetKey(KeyCode.LeftShift))
            {
                //The user clicked on the screen somewhere.
                if (Input.GetMouseButtonDown(0))
                {
                    //Declare variables needed.
                    Vector3 objPosition = new Vector3();
                    Vector3 mousePosition = new Vector3();
                    //Get the screen coordinates of the mouse and convert them to world coordinates.
                    mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10.0f);
                    objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                    //Add a new point to the current curve at the mouse coordinates.
                    allCurves[currentCurve].AddPoint(objPosition);
                }
            }
        }
    }

    //Display all information pertaining to the current curve in the upper right side of the screen.
    public void DisplayCurveInformation()
    {
        //Make sure we have a curve to display information about.
        if (allCurves.Count > 0)
        {
            curveInformation.text = null;
            curveInformation.text = "Bezier Curve #" + (currentCurve + 1) + Environment.NewLine;
            curveInformation.text += "Curve Resolution: " + allCurves[currentCurve].GetResulution() + Environment.NewLine;
            curveInformation.text += "Control Points: " + allCurves[currentCurve].GetNumControlPoints() + Environment.NewLine;
            for (int i = 0; i < allCurves[currentCurve].GetNumControlPoints(); i++)
            {
                string x = Math.Round(allCurves[currentCurve].GetControlPoints()[i].x, 2).ToString("F");
                string y = Math.Round(allCurves[currentCurve].GetControlPoints()[i].y, 2).ToString("F");
                curveInformation.text += (i + 1) + ".) (" + x + ", " + y + ")" + Environment.NewLine;
            }
        }
        else
        {
            //If there are no curves in the scene say so.
            curveInformation.text = "No curves in scene!";
        }
    }

    //Builds a new degree elevated version of the current curve and add it to the scene.
    public void RaiseBezierCurveDegree()
    {
        //If the user specified a particular number of time to preform the degree elevation.
        int tempVal, repeatedElevation;
        if (Int32.TryParse(degreeInput.text, out tempVal))
            repeatedElevation = tempVal;
        else
            repeatedElevation = 1;
        //Clear the input field.
        degreeInput.text = null;

        //The control polygon to manipulate must be of atleast degree 1.
        if (allCurves[currentCurve].GetNumControlPoints() > 1)
        {
            //Declare variables needed.
            List<Vector3> newControlPolygonPoints = new List<Vector3>();
            List<Vector3> originalControlPolygonPoints = new List<Vector3>(allCurves[currentCurve].GetControlPoints());

            for (int i = 0; i < repeatedElevation; i++)
            {
                //Calculate the point of the degree elevated control polygon.
                newControlPolygonPoints = allCurves[currentCurve].BezierDegreeElevation(originalControlPolygonPoints);
                //If more elevation is needed copy the points over.
                if((i + 1) < repeatedElevation)
                {
                    originalControlPolygonPoints = newControlPolygonPoints;
                }
            }

            //Create a new Bezier curve.
            BezierCurve degreeRaisedBezierCurve = new BezierCurve();
            //Add the calculated control points the new curve.
            for (int i = 0; i < newControlPolygonPoints.Count(); i++)
            {
                degreeRaisedBezierCurve.AddPoint(newControlPolygonPoints[i]);
            }
            //Set the resolution of the curve to that of the original.
            degreeRaisedBezierCurve.SetResolution(allCurves[currentCurve].GetResulution());
            //Add the new curve to the curves in the scene.
            allCurves.Add(degreeRaisedBezierCurve);
            //Select the newly added curve as the current curve.
            currentCurve = allCurves.Count - 1;
        }
    }

    //Builds a new degree elevated version of the current curve and add it to the scene.
    public void ReduceBezierCurveDegree()
    {
        //If the user specified a particular number of time to preform the degree elevation.
        int tempVal, repeatedReduction;
        if (Int32.TryParse(degreeInput.text, out tempVal))
            repeatedReduction = tempVal;
        else
            repeatedReduction = 1;
        //Clear the input field.
        degreeInput.text = null;

        //The control polygon to manipulate must be of atleast degree 1.
        if (allCurves[currentCurve].GetNumControlPoints() > 1)
        {
            //Declare variables needed.
            List<Vector3> newControlPolygonPoints = new List<Vector3>();
            List<Vector3> originalControlPolygonPoints = new List<Vector3>(allCurves[currentCurve].GetControlPoints());

            for (int i = 0; i < repeatedReduction; i++)
            {
                //Calculate the point of the degree elevated control polygon.
                newControlPolygonPoints = allCurves[currentCurve].BezierDegreeReduction(originalControlPolygonPoints);
                //If more elevation is needed copy the points over.
                if ((i + 1) < repeatedReduction)
                {
                    originalControlPolygonPoints = newControlPolygonPoints;
                }
            }

            //Create a new Bezier curve.
            BezierCurve degreeReducedBezierCurve = new BezierCurve();
            //Add the calculated control points the new curve.
            for (int i = 0; i < newControlPolygonPoints.Count(); i++)
            {
                degreeReducedBezierCurve.AddPoint(newControlPolygonPoints[i]);
            }
            //Set the resolution of the curve to that of the original.
            degreeReducedBezierCurve.SetResolution(allCurves[currentCurve].GetResulution());
            //Add the new curve to the curves in the scene.
            allCurves.Add(degreeReducedBezierCurve);
            //Select the newly added curve as the current curve.
            currentCurve = allCurves.Count - 1;
        }
    }

    //This method will find all the intersections of all curves in the scene.
    public void FindAllCurveIntersections()
    {
        //Declare a list to house all curve intersections it may find.
        List<Vector3> tempIntersectionList = new List<Vector3>();
        //Need at least two curves to have any intersections.
        if (allCurves.Count > 1)
        {
            //Compare all curve with all other curves.
            for (int i = 0; i < allCurves.Count; i++)
            {
                for (int j = 0; j < allCurves.Count; j++)
                {
                    //Don't compare a curve with itself.
                    if (i != j)
                    {
                        //Declare variables needed.
                        List<Vector3> curveIntersections = new List<Vector3>();
                        //Calculate intersections between each pair, if any.
                        curveIntersections = CalculateCurveIntersections(allCurves[i], allCurves[j]);
                        //Add the intersections to the list, if any.
                        for (int k = 0; k < curveIntersections.Count; k++)
                        {
                            //Add all intersection points to the main list of intersections.
                            tempIntersectionList.Add(curveIntersections[k]);
                        }
                    }
                }
            }

            //Remove all duplicate from the main list of intersections.
            for (int i = 0; i < tempIntersectionList.Count; i++)
            {
                //Make a flag that will decide when a point needs to be added.
                bool needToAddFlag = true;
                //Loop over all points that have already been added.
                for (int j = 0; j < allCurveIntersections.Count; j++)
                {
                    //If the distance between the points is very small we will consider them to be equal.
                    if (Vector3.Distance(tempIntersectionList[i], allCurveIntersections[j]) < 0.01f)
                    {
                        needToAddFlag = false;
                    }
                }
                //If the flag is not set then we need to ad the intersection point to the scene.
                if (needToAddFlag)
                {
                    allCurveIntersections.Add(tempIntersectionList[i]);
                }
            }
        }

        //Clear the value from the input field.
        thresholdInput.text = null;
    }

    //Determines if any two curves intersect.
    private List<Vector3> CalculateCurveIntersections(BezierCurve curveOne, BezierCurve curveTwo)
    {
        //Declare variables needed.
        List<BezierCurve> current = new List<BezierCurve>();
        List<BezierCurve> previous = new List<BezierCurve>();
        List<Vector3> curveIntersections = new List<Vector3>();

        //Assign the original pair of curves to the current list.
        current.Add(curveOne);
        current.Add(curveTwo);

        //If the user entered a value for the threshold use it, otherwise use a default.
        float tempVal, threshold;
        if (Single.TryParse(thresholdInput.text, out tempVal))
            threshold = tempVal;
        else
            threshold = 0.01f;
            
        while (current.Count != 0)
        {
            //Clear out the list from the last iteration.
            previous.Clear();
            //Copy the newest list.
            for (int i = 0; i < current.Count; i++)
            {
                previous.Add(current[i]);
            }
            //Clear the newest list.
            current.Clear();

            //Loop over each pair of curves and decide it it needs to be subdivded or discarded.
            for (int i = 0; i < previous.Count; i += 2)
            {
                //Do the bounding boxes each curve in this curve pair overlap.
                if (DetermineCurveIntersection(previous[i], previous[i + 1]))
                {
                    //Make some new curves to hold the newly subdivded curves.
                    BezierCurve rightCurveOne = new BezierCurve();
                    BezierCurve leftCurveOne = new BezierCurve();
                    BezierCurve rightCurveTwo = new BezierCurve();
                    BezierCurve leftCurveTwo = new BezierCurve();

                    //Subdived the first curve.
                    SubdivideSpecificBezierCurve(previous[i], ref rightCurveOne, ref leftCurveOne);
                    //Subdived the second curve.
                    SubdivideSpecificBezierCurve(previous[i + 1], ref rightCurveTwo, ref leftCurveTwo);

                    //Build First new pair.
                    current.Add(rightCurveOne);
                    current.Add(rightCurveTwo);
                    //Build second new pair.
                    current.Add(rightCurveOne);
                    current.Add(leftCurveTwo);
                    //Build third new pair.
                    current.Add(leftCurveOne);
                    current.Add(rightCurveTwo);
                    //Build fourth new pair.
                    current.Add(leftCurveOne);
                    current.Add(leftCurveTwo);
                }
            }
            
            //If there are subcurves.
            if(current.Count > 1)
            {
                //If the distance between the points of a subcurve are very small no more subdivisions are needed.
                if(Vector3.Distance(current[0].GetControlPoints().First(), current[1].GetControlPoints().First()) < threshold){
                    break;
                }
            }
        }

        //Add all the found intersections to a list to be returned.
        for (int i = 0; i < current.Count; i++)
        {
            curveIntersections.Add(current[i].GetControlPoints().First());
        }
        //Return the intersections found for this curve pair, if any.
        return curveIntersections;
    }

    //Determines if the bounding boxes for two curves have any overlap.
    private bool DetermineCurveIntersection(BezierCurve curveOne, BezierCurve curveTwo)
    {
        //Declare variables needed.
        Vector3 topLeftOne, lowerRightOne, topLeftTwo, lowerRightTwo;

        //Calculate the bounding boxes.
        CalculateCurveBoundingBox(curveOne, out topLeftOne, out lowerRightOne);
        CalculateCurveBoundingBox(curveTwo, out topLeftTwo, out lowerRightTwo);

        //Return a boolean value indicating if the two curves may have an intersection.
        return DetermineBoundingBoxOverlap(topLeftOne, lowerRightOne, topLeftTwo, lowerRightTwo);
    }

    //Calculate a min/max x and y values of a curve.
    private void CalculateCurveBoundingBox(BezierCurve curve, out Vector3 topLeft, out Vector3 lowerRight)
    {
        //Declare variables needed.
        float xMin = Single.MaxValue, xMax = Single.MinValue;
        float yMin = Single.MaxValue, yMax = Single.MinValue;

        //Loop over each point and update the min/max values as needed.
        for (int i = 0; i < curve.GetNumControlPoints(); i++)
        {
            float x = curve.GetControlPoints()[i].x;
            float y = curve.GetControlPoints()[i].y;
            xMin = Math.Min(xMin, x);
            xMax = Math.Max(xMax, x);
            yMin = Math.Min(yMin, y);
            yMax = Math.Max(yMax, y);
        }

        //Assign the calculated points.
        topLeft = new Vector3(xMin, yMax, 0.0f);
        lowerRight = new Vector3(xMax, yMin, 0.0f);
    }

    //Helper method for determining if the bounding boxes for two curves have any overlap.
    private bool DetermineBoundingBoxOverlap(Vector3 leftOne, Vector3 rightOne, Vector3 leftTwo, Vector3 rightTwo)
    {
        //If one rectanglular bounding box is on left side of other.
        if (leftOne.x > rightTwo.x || leftTwo.x > rightOne.x)
            return false;

        //If one rectanglular bounding box is above the other.
        if (leftOne.y < rightTwo.y || leftTwo.y < rightOne.y)
            return false;

        //If we get this far some portion of the bounding boxes overlap.
        return true;
    }

    //Used to subdivde specific curves for calculating curve intersections.
    private void SubdivideSpecificBezierCurve(BezierCurve curve, ref BezierCurve curveOutRight, ref BezierCurve curveOutLeft)
    {
        //Declare variables and objects needed.
        List<Vector3> intermediateControlPolygonPoints = new List<Vector3>();
        List<Vector3> leftCurve = new List<Vector3>();
        List<Vector3> rightCurve = new List<Vector3>();
        int N = curve.GetNumControlPoints() - 1;

        //Add the first at last points of the original control polygon to each appropriate curve.
        leftCurve.Add(curve.GetControlPoints().First());
        rightCurve.Add(curve.GetControlPoints().Last());

        //Call the DeCasteljau algorithm, each time effectively reduceing the number of edges in the original control polygon by one each iteration.
        for (int level = 1; level <= N; level++)
        {
            //Calculate the intermediate control points for each level, essentially stoping the DeCasteljau algorithm before it is complete.
            intermediateControlPolygonPoints = curve.CalculateIntermediateControlPolygon(0.5f, level);

            //If level == N then we only have one point, the curve point at t.
            if (level != N)
            {
                //Add the first point of each intermediate control polygon to the end of the left curve.
                leftCurve.Add(intermediateControlPolygonPoints.First());
                //Add the last point of each intermediate control polygon to the begining of the right curve.
                rightCurve.Insert(0, intermediateControlPolygonPoints.Last());
            }
            else
            {
                //Add the curve point at t to the end of the left curve.
                leftCurve.Add(intermediateControlPolygonPoints.First());
                //Add the curve point at t to the begining of the right curve.
                rightCurve.Insert(0, intermediateControlPolygonPoints.First());
            }
        }

        //Assgin the new right curve to the output reference parameter.
        curveOutRight = new BezierCurve();
        curveOutRight.SetResolution(curve.GetResulution());
        for (int j = 0; j < rightCurve.Count; j++)
            curveOutRight.AddPoint(rightCurve[j]);

        //Assign the new left curve to the output reference parameter.
        curveOutLeft = new BezierCurve();
        curveOutLeft.SetResolution(curve.GetResulution());
        for (int j = 0; j < leftCurve.Count; j++)
            curveOutLeft.AddPoint(leftCurve[j]);
    }

    //Select the next curve in the scene when the next button is clicked.
    public void SelectNextCurve()
    {
        //Update the currentCurve variable.
        if (allCurves.Count > 0)
            if (currentCurve == allCurves.Count - 1)
                currentCurve = 0;
            else
                currentCurve++;
        else
            currentCurve = 0;

        //Reset the time slider's value.
        timeSlider.value = 0;
    }

    //Select the previous curve in the scene when the prev button is clicked.
    public void SelectPreviousCurve()
    {
        //Update the current curve variable.
        if (allCurves.Count > 0)
            if (currentCurve == 0)
                currentCurve = allCurves.Count - 1;
            else
                currentCurve--;
        else
            currentCurve = 0;

        //Reset the time slider's value.
        timeSlider.value = 0;
    }

    //Increse the current curves resolution by incrementing the resolution value.
    public void IncreseCurveResolution()
    {
        if (allCurves.Count > 0)
            allCurves[currentCurve].SetResolution(allCurves[currentCurve].GetResulution() + 1);
    }

    //Decrese the the current curves resolution by decrementing the the resolution value.
    public void DecreaseCurveResolution()
    {
        if (allCurves.Count > 0)
            allCurves[currentCurve].SetResolution(allCurves[currentCurve].GetResulution() - 1);
    }

    //Toogles the draw method between DeCastlejau and Bernstein.
    public void ToggleDrawMethod()
    {
        drawMethod = drawMethodDropDown.value;
    }

    //Update the text diplayed above the time slider with the time sliders value.
    public void SetSliderValue()
    {
        //Format the string to three decimal places.
        sliderValueText.text = Math.Round(timeSlider.value, 3).ToString("F3");
    }

    //Destroy all game objects with a certain tag.
    private void DestroyGameObjectsWithTag(string tag)
    {
        //Find all the game objects with the parameter tag and destroy them.
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject gameObject in gameObjects)
            Destroy(gameObject);
    }

    //Adds a new curve to the scene and then makes it the current curve.
    public void AddCurve()
    {
        //Add the a new empty curve.
        BezierCurve newCurve = new BezierCurve();
        allCurves.Add(newCurve);
        //Select the new curve as the current curve.
        currentCurve = allCurves.Count - 1;
    }

    //Removes the current curve from the scene.
    public void RemoveCurve()
    {
        //If we have a curve to delete.
        if (allCurves.Count > 0)
        {
            //Remove the current curve.
            allCurves.RemoveAt(currentCurve);
            //Select the previous curve as the current curve.
            SelectPreviousCurve();
        }
    }

    //Adds a new point to the end of the current curve.
    public void AddPoint()
    {
        if(allCurves.Count > 0)
        {
            //Declare variables needed.
            float x, y;
            //Make sure that the user input can be converted to floats.
            if (Single.TryParse(xInput.text, out x) && Single.TryParse(yInput.text, out y))
            {
                //If the user input was valid add the new point to the current curve.
                allCurves[currentCurve].AddPoint(new Vector3(x, y, 0.0f));
                ClearInputFields();
            }
        }
    }

    //Adds a new point to the current curve at the desired index.
    public void InsertPoint()
    {
        if(allCurves.Count > 0)
        {
            //Declare variables needed.
            float x, y;
            int index;
            //Make sure that the user input can be converted to floats.
            if (Single.TryParse(xInput.text, out x) && Single.TryParse(yInput.text, out y) && Int32.TryParse(indexInput.text, out index))
            {
                //Make sure that the index entered by the user is valid.
                if (allCurves[currentCurve].validIndex(index - 1))
                {
                    //If the user input was valid add the new point to the current curve.
                    allCurves[currentCurve].InsertPoint(index - 1, new Vector3(x, y, 0.0f));
                    ClearInputFields();
                }
            }
        }
    }

    //Removes a point from the current curve at the desired index.
    public void RemovePoint()
    {
        if(allCurves.Count > 0)
        {
            //Declare variables needed.
            int index;
            //Make sure that the user input can be converted to floats.
            if (Int32.TryParse(indexInput.text, out index))
            {
                //Make sure that the index entered by the user is valid.
                if (allCurves[currentCurve].validIndex(index - 1))
                {
                    //If the user input was valid add the new point to the current curve.
                    allCurves[currentCurve].RemovePoint(index - 1);
                    ClearInputFields();
                }
            }
        }
    }

    //Clears the text from the three control point input fields.
    private void ClearInputFields()
    {
        xInput.text = null;
        yInput.text = null;
        indexInput.text = null;
    }

    //Load curve data from the file into the allCurves list.
    public void LoadCurveData()
    {
        //Clear out the old curve data.
        allCurves.Clear();
        //Load the curve data from the file.
        DataBase.LoadAllData(ref allCurves);
        //Reset the current curve variable.
        currentCurve = 0;
    }

    //Save curve data to the file from the allCurves list.
    public void SaveCurveData()
    {
        //Load the curve data from the file.
        DataBase.SaveAllData(allCurves);
    }

    //Removes all calculated curve intersections from the scene.
    public void RemoveCurveIntersections()
    {
        //Clear the list holding all current curve intersections.
        allCurveIntersections.Clear();
    }

    //Draw a series of grid lines in light gray in the background of the scene at 10 unit increments.
    private void DrawGridLines()
    {
        //Create a material for the grid line.
        Material gridLineMaterial = Resources.Load("GridLine", typeof(Material)) as Material;

        //Draw the verticals grip lines.
        for (float x = -150.0f; x <= 150.0f; x += 10.0f)
        {
            DrawLine(new Vector3(x, -100.0f, 0.0f), new Vector3(x, 100.0f, 0.0f), gridLineMaterial, -1, "GridLine", "GridLine");
        }

        //Draw the horizontal grid lines.
        for (float y = -90.0f; y <= 90.0f; y += 10.0f)
        {
            DrawLine(new Vector3(-150.0f, y, 0.0f), new Vector3(150.0f, y, 0.0f), gridLineMaterial, -1, "GridLine", "GridLine");
        }
    }
}


