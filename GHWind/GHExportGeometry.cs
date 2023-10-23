using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;

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
            pManager.AddGenericParameter("cubes as doubles", "cubes", "cubes as list of double[xmin, xmax, ymin, ymax, zmin, zmax]", GH_ParamAccess.list);
            pManager.AddTextParameter("path", "path", "path to export geometry data to", GH_ParamAccess.item);
            pManager.AddBooleanParameter("export?", "export?", "export data? use a button", GH_ParamAccess.item);
            pManager.AddTextParameter("&MESH text", "&MESH text","connect panel that contains the full command of the &MESH line in FDS format",GH_ParamAccess.item);//added
            pManager.AddTextParameter("&HEAD text", "&HEAD text", "connect panel that contains the full command of the &HEAD line in FDS format", GH_ParamAccess.item);//added
            pManager.AddTextParameter("&TIME text", "&TIME text", "connect panel that contains the full command of the &TIME line in FDS format", GH_ParamAccess.item);//added
            pManager.AddTextParameter("&DUMP text", "&DUMP text", "connect panel that contains the full command of the &DUMP line in FDS format", GH_ParamAccess.item);//added
            pManager.AddTextParameter("&TAIL text", "&TAIL text", "connect panel that contains the full command of the &TAIL line in FDS format", GH_ParamAccess.item);//added this line to take a text box as a new input
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

            string mytail = null;// added
            if (!DA.GetData(7, ref mytail)) { return; };//added these two lines to get the data from the new grasshopper parameter input


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

                List<string> myheadlist = new List<string>();
                myheadlist.Add(myhead); 

                List<string> mytimelist = new List<string>();
                mytimelist.Add(mytime);

                List<string>mydumplist = new List<string>();
                mydumplist.Add(mydump);

                List<string> mytaillist = new List<string>();
                mytaillist.Add(mytail);

                var ht = myheadlist.Concat(mytimelist);
                var htd = ht.Concat(mydumplist);


                List<string> mymeshlist = new List<string>(); //added
                mymeshlist.Add(mymesh); //added
               
                
                var htdm = htd.Concat(mymeshlist); //added these three lines to take the input from the GetData and combine the two lists

                var htdmo = htdm.Concat(list);

                var htdmot = htdmo.Concat(mytaillist);

                lines = htdmot.ToArray(); //changed this line from lines = list.ToArray()

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