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

/*
 * GHExportGeometry.cs
 * Copyright 2017 Christoph Waibel <chwaibel@student.ethz.ch>
 * 
 * This work is licensed under the GNU GPL license version 3.
*/

namespace GHWind
{
    public class GHExportGeometry : GH_Component
    {

        bool export;

        public GHExportGeometry()
            : base("Export Geometry",  "Export geometry",
                "Export geometry into a *.csv file",
                "EnergyHubs", "Wind Simulation")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("cubes as doubles", "cubes", "cubes as list of double[xmin, xmax, ymin, ymax, zmin, zmax]", GH_ParamAccess.list);//0
            pManager.AddTextParameter("path", "path", "path to export geometry data to", GH_ParamAccess.item);//1
            pManager.AddBooleanParameter("export?", "export?", "export data? use a button", GH_ParamAccess.item);//2
            pManager.AddTextParameter("&MESH text", "&MESH text","connect panel that contains the full command of the &MESH line in FDS format",GH_ParamAccess.item);//3
            pManager.AddTextParameter("&HEAD text", "&HEAD text", "connect panel that contains the full command of the &HEAD line in FDS format", GH_ParamAccess.item);//4
            pManager.AddTextParameter("&TIME text", "&TIME text", "connect panel that contains the full command of the &TIME line in FDS format", GH_ParamAccess.item);//5
            pManager.AddTextParameter("&DUMP text", "&DUMP text", "connect panel that contains the full command of the &DUMP line in FDS format", GH_ParamAccess.item);//6
            pManager.AddSurfaceParameter("Inlet Surfaces", "Inlet Surfaces", "List of inlet surfaces (rectangular)", GH_ParamAccess.list);//7
            pManager.AddSurfaceParameter("Outlet Surfaces", "Outlet Surfaces", "List of outlet surfaces (rectangular)", GH_ParamAccess.list);//8
            pManager.AddPointParameter("origin", "origin", "origin", GH_ParamAccess.item);//9
            
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<double[]> geom = new List<double[]>();
            if (!DA.GetDataList(0, geom)) { return; };


            string path = null;
            if (!DA.GetData(1, ref path)) { return; };


            DA.GetData(2, ref export);

            string mymesh = null;// added
            if(!DA.GetData(3, ref mymesh)) { return; }; //added these two lines to get the data from the new grasshopper parameter input

            string myhead = null;// added
            if (!DA.GetData(4, ref myhead)) { return; };//added these two lines to get the data from the new grasshopper parameter input

            string mytime = null;// added
            if (!DA.GetData(5, ref mytime)) { return; };//added these two lines to get the data from the new grasshopper parameter input

            string mydump = null;// added
            if (!DA.GetData(6, ref mydump)) { return; };//added these two lines to get the data from the new grasshopper parameter input

            List<Surface> inlets = new List<Surface>();
            if (!DA.GetDataList(7, inlets)) { return; };

            List<Surface> outlets = new List<Surface>();
            if (!DA.GetDataList(8, outlets)) { return; }

            Point3d origin = Point3d.Unset;
            if (!DA.GetData(9, ref origin)) { return; }

            //List<Point3d> myinlet = new List<Point3d>();
            //if (!DA.GetDataList(8, myinlet)) {  return; };


            //EXPORT GEOMETRY
            if (export)
            {

                string[] lines;
                var list = new List<string>();
                foreach (double[] geo in geom)
                {
                    string line = "&OBST XB=" + geo[0].ToString() + "," + geo[1].ToString() + "," + geo[2].ToString() + "," + geo[3].ToString() + "," + geo[4].ToString() + "," + geo[5].ToString() +" , SURF_ID='INERT' /";
                    list.Add(line);
                }

                var myinletlist = new List<string>();
                foreach (Surface inlet in inlets)
                {
                    BoundingBox inletbox = inlet.GetBoundingBox(true);
                    Point3d[] inletcorner = inletbox.GetCorners(); 
                    string inletboxline = "&VENT XB=" + (inletcorner[0][0] - origin[0]).ToString() + "," + (inletcorner[1][0]-origin[0]).ToString() + "," + (inletcorner[0][1] - origin[1]).ToString() + "," + (inletcorner[2][1] - origin[1]).ToString() + "," + (inletcorner[0][2] - origin[2]) + "," + (inletcorner[4][2] - origin[2]).ToString() + ", COLOR = 'GREEN', SURF_ID = 'supply' //" ;
                    myinletlist.Add(inletboxline);
                }

                var myoutletlist = new List<string>();
                foreach (Surface outlet in outlets)
                {
                    BoundingBox outletbox = outlet.GetBoundingBox(true);
                    Point3d[] outletcorner = outletbox.GetCorners();
                    string outletboxline = "&VENT XB=" + (outletcorner[0][0] - origin[0]).ToString() + "," + (outletcorner[1][0] - origin[0]).ToString() + "," + (outletcorner[0][1] - origin[1]).ToString() + "," + (outletcorner[2][1] - origin[1]).ToString() + "," + (outletcorner[0][2] - origin[2]) + "," + (outletcorner[4][2] - origin[2]).ToString() + ", COLOR = 'RED', SURF_ID = 'exhaust' //";
                    myoutletlist.Add(outletboxline);
                }


                List<string> myheadlist = new List<string>();
                myheadlist.Add(myhead); 

                List<string> mytimelist = new List<string>();
                mytimelist.Add(mytime);

                List<string>mydumplist = new List<string>();
                mydumplist.Add(mydump);

                string mytail = "&TAIL //";
                List<string> mytaillist = new List<string>();
                mytaillist.Add(mytail);

                var ht = myheadlist.Concat(mytimelist);
                var htd = ht.Concat(mydumplist);


                List<string> mymeshlist = new List<string>(); //added
                mymeshlist.Add(mymesh); //added
               
                
                var htdm = htd.Concat(mymeshlist); //added these three lines to take the input from the GetData and combine the two lists

                var htdmo = htdm.Concat(list);

                var htdmoi = htdmo.Concat(myinletlist);
                
                var htdmoiu = htdmoi.Concat(myoutletlist);

                var htdmoiut = htdmoiu.Concat(mytaillist);

                lines = htdmoiut.ToArray(); //changed this line from lines = list.ToArray()

                File.WriteAllLines(path, lines);
                export = false;

               
            }

            

        }




        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return GHWind.Properties.Resources.discr_export;
            }
        }



        public override Guid ComponentGuid
        {
            get { return new Guid("{9d1d9ace-f25a-4bf8-8282-660335fd2bd5}"); }
        }
    }
}