using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

namespace Curve
{
    public static class DataBase
    {
        public static void LoadAllData(ref List<BezierCurve> allCurves)
        {
            //Declare variables needed.
            StreamReader fin = new StreamReader(@".\bezierDataBase.txt", Encoding.Default);
            //Remove all the old curves.
            allCurves.Clear();

            //Make sure we have some curves in the file to process.
            int numCurves = Convert.ToInt32(fin.ReadLine());
            if (numCurves > 0)
            {
                //Loop over each curve in the file and store the necessary information.
                for (int i = 0; i < numCurves; i++)
                {
                    //Decalre a new BezierCurve
                    BezierCurve newBezierCurve = new BezierCurve();
                    //Get the curves current resolution from the file.
                    int resolution = Convert.ToInt32(fin.ReadLine());
                    newBezierCurve.SetResolution(resolution);
                    //Get the number of control points in the curve from the file.
                    int numControlPoints = Convert.ToInt32(fin.ReadLine());
                    for (int j = 0; j < numControlPoints; j++)
                    {
                        //Add each control point to the new curve.
                        string[] coordinates = fin.ReadLine().Split(null);
                        float x = Convert.ToSingle(coordinates[0]);
                        float y = Convert.ToSingle(coordinates[1]);
                        newBezierCurve.AddPoint(new Vector3(x, y, 0.0f));
                    }

                    allCurves.Add(newBezierCurve);
                }
            }
            //Close the file.
            fin.Close();
        }

        //Write all curves in the current sence to the file.
        public static void SaveAllData(List<BezierCurve> allCurves)
        {
            //Declare variables needed.
            StreamWriter fout = new StreamWriter(@".\bezierDataBase.txt", false, Encoding.Default);
            //Write the number of curves in the scene to the file.
            fout.WriteLine(allCurves.Count);
            //Loop over each curve.
            for(int i = 0; i < allCurves.Count; i++)
            {
                //Write the curves current resolution to the file.
                fout.WriteLine(allCurves[i].GetResulution());
                //Write the number of control points to the file.
                fout.WriteLine(allCurves[i].GetNumControlPoints());
                //Loop over all points in the curve.
                for (int j = 0; j < allCurves[i].GetNumControlPoints(); j++)
                {
                    //Format the coordinate information.
                    string x = Math.Round(allCurves[i].GetControlPoints()[j].x, 2).ToString("F");
                    string y = Math.Round(allCurves[i].GetControlPoints()[j].y, 2).ToString("F");
                    //Write each set of coordinates on a sperate line.
                    fout.WriteLine(x + " " + y);
                }
            }

            //Close the file
            fout.Close();
        }
    }
}
