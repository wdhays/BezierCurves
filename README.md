# BezierCurves

This is a UNITY 3D project, I have included a build for Windows 10 in the “Builds” folder, just run Builds.exe. I suggest you run it on “fantastic” quality.

## Program Instructions:
1.	The scene will begin with a blank canvas.
2.	To add a curve you must first click the “Add Curve” button to create an empty curve (no control points). You can add as many curves as you would like.
3.	To add points to the curve there are two methods. You can enter exact (X,Y) values in the input boxes labeled X and Y, then click the “Add Point” button. Alternatively you can hold done the left shift key and right click anywhere on the canvas to add a control point at that location. New control points added with either of these two methods will be inserted at the end of the control point list.
4.	Similarly there is an “Insert Point” button that will let you add a control point at a certain index in the control point list. There is a third input field next to the X and Y input fields for Index specification.
5.	The “Remove Point” button will remove the control point at the specified index.
6.	When dealing with multiple curves the “Next Curve” and “Previous Curve” buttons will cycle through the curves in the scene. The current curves control polygon is drawn in red while the others are drawn in black.
7.	You can move any control point in the current curve by clicking and holding down the left mouse over a control point, then dragging it to the desired location. Note: Won’t work when holding left shift (adding new control points).
8.	You can increase or decrease the current curves resolution by using the “Decrease Resolution” and “Increase Resolution” buttons.
9.	The time slider on the right will trace the curve, and all intermediate control polygons will be drawn showing the stages of the de Castlejau algorithm.
10.	You can save the scene with the “Save Curves” button or load from a file with the “Load Curves” button.
11.	The current curve can be removed from the scene using the “Remove Curve” button.
12.	Intersections can be calculated using the “Find Curve Intersections” button and the threshold input field(can be let blank), each intersection will be marked with a red “X”. Curve intersections can be removed with the “Remove Intersections” button.
13.	All information about the current curve will be displayed in the top right of the window. Information includes the current resolution, number of control points, and the location of each control point.
14.	You can elevate/reduce the curves degree using the “Raise Degree” and “Reduce Degree” buttons, there is also an input field for entering a number for repeated degree elevation/reduction. A new curve with the raised/reduced degree will be added, the original will not be removed.
15.	You can subdivide the current curve into two separate curves with the same degree as the original by using the t slider to specify the desired location to split the curve then hitting the “Subdivide Curve” button.

## Algoithm Stuff:
1.	Most of the Bezier Curve implementation is in Assets/Scripts/BezierCurve.cs, including the DeCastlejau(121-150), Bernstien(202-235), the Intermediate Polygons(153-178), and a single Curve Point at T(181-199), Degree Elevation(261-284), and Degree Reduction(287-336).
2.	Assets/Scripts/Database.cs just has two functions for saving and loading to a test file.
3.	All other implementation is in Assets/Scripts/ProgramManager.cs, including all the UI callbacks, mouse functions, and keyboard functions, wrappers for the Assets/Scripts/BezierCurve.cs functions, Intersection functions (466-653, 6 functions), Curve Subdivision (656-702), and all Draw functions (67-194).

