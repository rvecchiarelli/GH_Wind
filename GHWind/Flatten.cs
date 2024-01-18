using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Drawing;
using GH_IO.Serialization;
using System.Net.NetworkInformation;
using Rhino.Geometry.Collections;
using Rhino.Collections;
using System.Drawing.Text;
using Grasshopper.Kernel.Geometry;
using Rhino.Input.Custom;
using Grasshopper.Documentation;

/*
 * GHExportGeometry.cs
 * Copyright 2017 Christoph Waibel <chwaibel@student.ethz.ch>
 * 
 * This work is licensed under the GNU GPL license version 3.
*/

namespace GHWind
{
 
    public class Flatten : GH_Component
    {

        bool export;

        public Flatten()
            : base("Flatten", "Flatten",
                "Export geometry as list of nodes",
                "EnergyHubs", "Wind Simulation")
        {
        }


        public static List<Point3d> SnapSurfs(List<Surface> surfs, List<Point3d> nodes)
        {
            List<string> geoStrings = new List<string>();
            List<Point3d> snappedVerts = new List<Point3d>();
            List<BoundingBox> snappedSurfs = new List<BoundingBox>();
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(10, 10, 0, 0);

            foreach (Surface surf in surfs)
            {
                Brep brep = surf.ToBrep();
                BoundingBox surfBox = surf.GetBoundingBox(true);
                var surfCorners = surfBox.GetCorners();

                foreach (Point3d corner in surfCorners)
                {
                    var newVerts = corner;
                    if (nodes.Contains(newVerts) == false)
                    {
                        Point3d closestPoint = Point3dList.ClosestPointInList(nodes, newVerts);
                        newVerts = closestPoint;
                    }
                    else
                    {
                        newVerts = newVerts;
                    }
                    
                    snappedVerts.Add(newVerts);
                    
                }

                
                
                BoundingBox snappedBox = new BoundingBox(snappedVerts);
                snappedSurfs.Add(snappedBox);
                snappedVerts.Clear();
            }


            //Makes a list of all the points in the list of mesh nodes that are contained within a surface
            List<Point3d> surfPoint = new List<Point3d>();
            foreach (BoundingBox surf in snappedSurfs)
            {
                var point = surf.GetCorners();
                List<Point3d> distinct = point.Distinct().ToList();

                List<double> xValues = new List<double>();
                List<double> yValues = new List<double>();
                List<double> zValues = new List<double>();
                foreach (Point3d d in distinct)
                {
                    xValues.Add(d.X);
                    yValues.Add(d.Y);
                    zValues.Add(d.Z);
                }

                foreach (Point3d node in nodes)
                {
                    
                    if ((node.X <= xValues.Max() && xValues.Min() <= node.X) && (node.Y <= yValues.Max() && yValues.Min() <= node.Y) && (node.Z <= zValues.Max() && zValues.Min() <= node.Z))
                    {
                        surfPoint.Add(node);
                    }
                }

            }

            return surfPoint;
        }





        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddBooleanParameter("export?", "export?", "export data? use a button", GH_ParamAccess.item);
            //1
            pManager.AddTextParameter("path", "path", "path to export geometry data to", GH_ParamAccess.item);
            //2
            pManager.AddPointParameter("origin", "origin", "origin", GH_ParamAccess.item);
            //3
            pManager.AddBoxParameter("bounding box", "box", "bounding box as brep", GH_ParamAccess.item);
            //4
            pManager.AddBoxParameter("obstacles", "obstacles", "obstacles as box list(discr mesh output)", GH_ParamAccess.list);
            pManager[4].Optional = true;
            //5
            pManager.AddSurfaceParameter("Inlet Surfaces", "Inlet Surfaces", "List of inlet surfaces (rectangular)", GH_ParamAccess.list);
            //6
            pManager.AddSurfaceParameter("Outlet Surfaces", "Outlet Surfaces", "List of outlet surfaces (rectangular)", GH_ParamAccess.list);
            //7
            pManager.AddIntegerParameter("IJK", "I J K", "Number of cells in the X, Y, and Z directions as a list.", GH_ParamAccess.list);
            //8
            pManager.AddNumberParameter("Supply Velocity", "Supply Velocity", "Supply velocity in m/s", GH_ParamAccess.item);
            //9
            pManager.AddNumberParameter("Exhaust Velocity", "Exhaust Velocity", "Velocity flow in m/s", GH_ParamAccess.item);
            //10
            pManager.AddIntegerParameter("Case Index", "Index", "Index of the case to write as an integer", GH_ParamAccess.item);
            //11
            pManager.AddIntegerParameter("Vent Type", "Vent Type", "Coefficients for Vent Type. 0 = Linear; 1 = Louvered; 2 = Radial; 3 = Spiral.", GH_ParamAccess.item);


        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetData(0, ref export);

            string path = null;
            if (!DA.GetData(1, ref path)) { return; };

            Point3d origin = Point3d.Unset;
            if (!DA.GetData(2, ref origin)) { return; }

            List<Box> obstacles = new List<Box>();
            DA.GetDataList(4,obstacles);

            List<int> ijk = new List<int>();
            if (!DA.GetDataList(7, ijk)) { return; };

            Box box = new Box();
            if (!DA.GetData(3, ref box)) { return; }

            double supply = 1.0;
            if (!DA.GetData(8, ref supply)) { return; };

            double exhaust = 1.0;
            if (!DA.GetData(9, ref exhaust)) { return; };

            List<Surface> inlets = new List<Surface>();
            if (!DA.GetDataList(5, inlets)) { return; };

            List<Surface> outlets = new List<Surface>();
            if (!DA.GetDataList(6, outlets)) { return; }

            int index = 0;
            if (!DA.GetData(10, ref index)) { return; };

            int venttype = 0;
            if (!DA.GetData(11, ref venttype)) { return; };


            //Mimic FDS geom.f90  snappingGeom subroutine

            //Take the bounding box and get the dimensions. Then take the number of cells to get the number of nodes in each dimension.
            //Use these values to create a grid of points that match the FDS mesh.
            var boxCorners= box.GetCorners();
            double cellLength = (boxCorners[6].X - boxCorners[0].X) / ijk[0];
            double cellWidth = (boxCorners[6].Y - boxCorners[0].Y) / ijk[1];
            double cellHeight = (boxCorners[6].Z - boxCorners[0].Z) / ijk[2];

            int xnodes = ijk[0] + 1;
            int ynodes = ijk[1] + 1;
            int znodes = ijk[2] + 1;

            List<Point3d> nodes = new List<Point3d>();

            for (int x = 0; x < xnodes; x++)
            {
                for (int y = 0; y < ynodes; y++)
                {
                    for (int z = 0; z < znodes; z++)
                    {
                        nodes.Add(new Point3d((double) x * cellLength,(double) y * cellWidth,(double) z * cellHeight));
                    }
                }
            }

            //Make a list of all the nodes on the surface of the box

            List<Point3d> edgeNodes = new List<Point3d>();
            foreach (Point3d node in nodes)
            {
                if (node.X == boxCorners[0].X | node.X == boxCorners[6].X | node.Y == boxCorners[0].Y | node.Y == boxCorners[6].Y | node.Z == boxCorners[0].Z | node.Z == boxCorners[6].Z)
                {
                    edgeNodes.Add(node);
                }
            }





            //Take the obstacles boxes and the origin point and remap the boxes to an origin of 0,0,0.
            //For each point in the new list of obstacle points, check if that point exists in the list of mesh nodes
            //(therefore is the obstacle aligned with a mesh cell) if not, find the nearest point and replace the unaligned point
            //with a point that is aligned to the mesh and create a new obstacle box.
            //
            List<Point3d> obstPoint = new List<Point3d>();
            List<string> geoStrings = new List<string>();
            if (obstacles.Count() != 0)
            {
                

                List<Point3d> snappedVerts = new List<Point3d>(); //

                List<string> verts = new List<string>();

                List<Box> snappedObsts = new List<Box>();

                Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(10, 10, 0, 0);

                foreach (Box obst in obstacles)
                {
                    var obstCorners = obst.GetCorners();


                    foreach (Point3d corner in obstCorners)
                    {

                        Point3d newVerts = corner;

                        if (nodes.Contains(newVerts) == false)
                        {
                            Point3d closestPoint = Point3dList.ClosestPointInList(nodes, newVerts);
                            newVerts = closestPoint;
                        }
                        else
                        {
                            newVerts = newVerts;
                        }
                        snappedVerts.Add(newVerts);

                    }

                    Box snappedBox = new Box(plane, snappedVerts);
                    snappedObsts.Add(snappedBox);
                    snappedVerts.Clear();

                }



                //Makes a list of all the points in the list of mesh nodes that are contained within an obstacle
                
                foreach (Box obst in snappedObsts)
                {
                    Brep brepObst = obst.ToBrep();
                    foreach (Point3d node in nodes)
                    {

                        if (brepObst.IsPointInside(node, 0.0001, true) == true)
                        {
                            obstPoint.Add(node);
                        }
                    }

                }


            }//End of if Geo != 0

            //Creates a new array in the format [X-coordinate, Y-coordinate, Z-coordinate, Definition] where definition
            //denotes whether or not that node is contained within or along the edge of an obstacle
            var geoList = new List<GeoArray>();
            List<Point3d> inletPoints = SnapSurfs(inlets, edgeNodes);
            List<Point3d> outletPoints = SnapSurfs(outlets, edgeNodes);

            foreach (Point3d node in nodes)
            {
                if (obstPoint.Contains(node) == true)
                {
                    geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 6 });
                }
                else if(inletPoints.Contains(node) == true)
                {
                    switch(venttype)
                    {
                        case 0:
                            geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 2 });
                            break;
                        case 1:
                            geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 3 });
                            break;
                        case 2:
                            geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 4 });
                            break;
                        case 3:
                            geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 5 });
                            break;
                    }
                    
                }
                else if(outletPoints.Contains(node)==true)
                {
                    geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 1 });
                }
                else
                {
                    geoList.Add(new GeoArray { X = node[0], Y = node[1], Z = node[2], D = 0 });
                }
            }

            //Writes the GeoArray to a comma delimmited list
            foreach (GeoArray item in geoList)
            {
                string geoString = $"{ item.X.ToString() },{ item.Y.ToString()},{ item.Z.ToString()},{ item.D.ToString()}";
                geoStrings.Add(geoString);
            }


            var geoArray = geoStrings.ToArray();



            //EXPORT GEOMETRY to a csv file
            if (export)
            {
                string filepath = $@"{path}\{index.ToString()}.csv";
                var currentfile = File.Create(filepath);
                currentfile.Close();
                File.WriteAllLines(filepath, geoArray);
                export = false;


            }



        }




        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return GHWind.Properties.Resources.flatten;
            }
        }



        public override Guid ComponentGuid
        {
            get { return new Guid("{591117fb-0e16-4289-80ac-3876158dba3e}"); }
        }
    }
}